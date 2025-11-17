
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;



namespace GoveKits.Network
{
    /// <summary>
    /// Protocol，ID + Length + DataBody = 4 + 4 + ...
    /// </summary>
    public static partial class MessageBuilder
    {
        // store factories that create a fresh Message instance per request
        private static Dictionary<int, Func<Message>> factories = new();

        // Convenience: register a prototype instance; the builder will create new instances
        // using the prototype's concrete type via Activator.CreateInstance.
        public static void AddMessage(int keyID, Message prototype)
        {
            if (prototype == null) throw new ArgumentNullException(nameof(prototype));
            factories[keyID] = () => (Message)Activator.CreateInstance(prototype.GetType());
        }

        // Return a fresh Message instance for the given id, or null if not registered
        public static Message GetMessage(int keyID)
        {
            if (factories.TryGetValue(keyID, out var f))
            {
                try { return f(); }
                catch (Exception ex)
                {
                    Debug.LogError($"MessageBuilder.GetMessage factory for {keyID} threw: {ex}");
                    return null;
                }
            }
            Debug.LogWarning($"MessageBuilder: message id {keyID} not registered");
            return null;
        }
    }


    public abstract class MessageData
    {
    // 数据
    // 。。。。。。
    // 重写
    public abstract int ByteLength();
    public abstract byte[] Writing();
    public abstract int Reading(byte[] bytes, int beginIndex = 0);
    // 写
    protected void WriteInt(byte[] bytes, int value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(int);
    }
    protected void WriteShort(byte[] bytes, short value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(short);
    }
    protected void WriteLong(byte[] bytes, long value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(long);
    }
    protected void WriteFloat(byte[] bytes, float value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(float);
    }
    protected void WriteByte(byte[] bytes, byte value, ref int index)
    {
        bytes[index] = value;
        index += sizeof(byte);
    }
    protected void WriteBool(byte[] bytes, bool value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(bool);
    }
    protected void WriteString(byte[] bytes, string value, ref int index)
    {
        //先存储string字节数组的长度
        byte[] strBytes = Encoding.UTF8.GetBytes(value);
        //BitConverter.GetBytes(strBytes.Length).CopyTo(bytes, index);
        //index += sizeof(int);
        WriteInt(bytes, strBytes.Length, ref index);
        //再存 string字节数组
        strBytes.CopyTo(bytes, index);
        index += strBytes.Length;
    }
    protected void WriteData(byte[] bytes, MessageData data, ref int index)
    {
        data.Writing().CopyTo(bytes, index);
        index += data.ByteLength();
    }
    // 读
    protected int ReadInt(byte[] bytes, ref int index)
    {
        int value = BitConverter.ToInt32(bytes, index);
        index += sizeof(int);
        return value;
    }
    protected short ReadShort(byte[] bytes, ref int index)
    {
        short value = BitConverter.ToInt16(bytes, index);
        index += sizeof(short);
        return value;
    }
    protected long ReadLong(byte[] bytes, ref int index)
    {
        long value = BitConverter.ToInt64(bytes, index);
        index += sizeof(long);
        return value;
    }
    protected float ReadFloat(byte[] bytes, ref int index)
    {
        float value = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        return value;
    }
    protected byte ReadByte(byte[] bytes, ref int index)
    {
        byte value = bytes[index];
        index += sizeof(byte);
        return value;
    }
    protected bool ReadBool(byte[] bytes, ref int index)
    {
        bool value = BitConverter.ToBoolean(bytes, index);
        index += sizeof(bool);
        return value;
    }
    protected string ReadString(byte[] bytes, ref int index)
    {
        //首先读取长度
        int length = ReadInt(bytes, ref index);
        //再读取string
        string value = Encoding.UTF8.GetString(bytes, index, length);
        index += length;
        return value;
    }
    protected T ReadData<T>(byte[] bytes, ref int index) where T: MessageData, new()
    {
        T value = new T();
        index += value.Reading(bytes, index);
        return value;
    }
    }


    public abstract class Message : MessageData
    {
        // 消息ID
        public int MsgID;
        public MessageData MsgData;

        // 长度 => ID + DataBodyLength + DataBody
        public override int ByteLength() => sizeof(int) + sizeof(int) + MsgData.ByteLength();
        // 序列化为字节数组（用于发送），可以返回 null/empty
        public override byte[] Writing()
        {
            int idx = 0;
            byte[] bytes = new byte[ByteLength()];
            WriteInt(bytes, MsgID, ref idx);
            WriteInt(bytes, bytes.Length - 8, ref idx);
            WriteData(bytes, MsgData, ref idx);
            return bytes;
        }
        // 反序列化消息体，offset 为消息体起始索引
        public override int Reading(byte[] buffer, int offset)
        {  
            int idx = offset;
            // 这里的数据在前面读取过了，不需要再读
            // MsgID = ReadInt(buffer, ref idx);
            // var _len = ReadInt(buffer, ref idx);
            idx += MsgData.Reading(buffer, idx);
            return idx - offset;
        }
        // 处理消息的异步方法
        public abstract UniTask Handle();
    }


    public class NetSocket
    {
        private Socket socket;  // 底层 Socket 对象
        private byte[] cacheBytes = new byte[1024 * 1024];  // 1MB 缓存区
        private int cacheNum = 0;  // 缓存区已用字节数


        private Queue<Message> receiveQueue = new Queue<Message>();  // 接收消息队列

        private readonly object receiveLock = new object();  // 接收队列锁
        private readonly SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);  // 发送信号量


        public float heartbeatInterval = 10f;  // 心跳间隔（秒）



        public async UniTask Process()
        {
            try
            {
                if (socket == null)
                    return;

                while (true)
                {
                    Message msg = null;
                    lock (receiveLock)
                    {
                        if (receiveQueue.Count > 0)
                            msg = receiveQueue.Dequeue();
                    }

                    if (msg == null)
                        break;

                    try
                    {
                        // Debug.Log($"[NetSocket] Handling message id={msg.MsgID}");
                        await msg.Handle();
                        // Debug.Log($"[NetSocket] Handled message id={msg.MsgID}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NetSocket] Handle message id={msg.MsgID} error: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetSocket] {ex}");
            }
        }


        /// <summary>
        /// 1.2. 创建并连接到服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public NetSocket(string host, int port, Action callbackSuccess = null, Action callbackFailure = null)
        {
            // 1.创建 Socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 2.连接服务器
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(host), port);
            args.RemoteEndPoint = ipPoint;
            args.Completed += (s, e) =>
            {
                if(e.SocketError == SocketError.Success)
                {
                    // Debug.Log("连接成功");
                    callbackSuccess?.Invoke();
                    Debug.Log($"[NetSocket] Connected to {host}:{port}");
                    //收消息
                    SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                    receiveArgs.SetBuffer(cacheBytes, 0, cacheBytes.Length);
                    receiveArgs.Completed += Receive;
                    this.socket.ReceiveAsync(receiveArgs);
                }
                else
                {
                    // Debug.Log("连接失败" + args.SocketError);
                    Debug.LogWarning($"[NetSocket] Connect failed: {e.SocketError}");
                    callbackFailure?.Invoke();
                }
            };
            socket.ConnectAsync(args);

        }

        /// <summary>
        /// 3. 发送数据
        /// </summary>
        public async UniTask Send(Message msg)
        {
            if (socket == null)
                throw new InvalidOperationException("Socket is null or closed");

            var data = msg.Writing();
            if (data == null || data.Length == 0)
                return;

            await sendSemaphore.WaitAsync();
            try
            {
                // run blocking send on threadpool to avoid blocking Unity main thread
                await UniTask.RunOnThreadPool(() =>
                {
                    int sent = 0;
                    while (sent < data.Length)
                    {
                        int s = socket.Send(data, sent, data.Length - sent, SocketFlags.None);
                        if (s <= 0) break;
                        sent += s;
                    }
                });
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 4. 接收数据
        /// </summary>
        /// <param name="handler"></param>
        public void Receive(object obj, SocketAsyncEventArgs args)
        {
            Debug.Log($"[NetSocket] Receive callback: SocketError={args.SocketError}, BytesTransferred={args.BytesTransferred}");
            if(args.SocketError == SocketError.Success)
            {
                HandleReceiveMsg(args.BytesTransferred);
                //继续去收消息
                try
                {
                    args.SetBuffer(cacheNum, args.Buffer.Length - cacheNum);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetSocket] SetBuffer error: {ex}");
                }

                //继续异步收消息
                if (this.socket != null && this.socket.Connected)
                    socket.ReceiveAsync(args);
                else
                    Close();
            }
            else
            {
                Debug.LogWarning($"[NetSocket] 接受消息出错 {args.SocketError}");
                //关闭客户端连接
                Close();
            }
        }

        /// <summary>
        /// 5. 关闭连接
        /// </summary>
        public void Close()
        {
            socket?.Close();
        }




        //处理接受消息 分包、黏包问题的方法
    private void HandleReceiveMsg(int receiveNum)
    {
        int msgID = 0;
        int msgLength = 0;
        int nowIndex = 0;

        cacheNum += receiveNum;
        while (true)
        {
            //每次将长度设置为-1 是避免上一次解析的数据 影响这一次的判断
            msgLength = -1;
            //处理解析一条消息
            if (cacheNum - nowIndex >= 8)
            {
                //解析ID
                msgID = BitConverter.ToInt32(cacheBytes, nowIndex);
                nowIndex += 4;
                //解析长度
                msgLength = BitConverter.ToInt32(cacheBytes, nowIndex);
                nowIndex += 4;
            }

            if (cacheNum - nowIndex >= msgLength && msgLength != -1)
            {
                Debug.Log($"[NetSocket] Parsed msg header: id={msgID}, length={msgLength}, nowIndex={nowIndex}");
                //得到一个指定ID的消息类对象（合并 Message + 处理）
                Message msg = MessageBuilder.GetMessage(msgID);
                if (msg != null)
                {
                    // 反序列化消息体到 Message
                    try
                    {
                        if (msg.MsgData == null)
                        {
                            Debug.LogError($"[NetSocket] msg.MsgData is null for msgID={msgID} - cannot deserialize");
                        }
                        msg.Reading(cacheBytes, nowIndex);
                        // 把 Message 放入队列，稍后通过 ProcessAsync 处理
                        lock (receiveLock)
                        {
                            receiveQueue.Enqueue(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NetSocket] Exception while Reading msgID={msgID}: {ex}");
                    }
                }

                nowIndex += msgLength;
                if (nowIndex == cacheNum)
                {
                    cacheNum = 0;
                    // Debug.Log("[NetSocket] All data consumed, cacheNum reset to 0");
                    break;
                }
            }
            else
            {
                if (msgLength != -1)
                    nowIndex -= 8;
                //就是把剩余没有解析的字节数组内容 移到前面来 用于缓存下次继续解析
                Array.Copy(cacheBytes, nowIndex, cacheBytes, 0, cacheNum - nowIndex);
                cacheNum = cacheNum - nowIndex;
                break;
            }
        }

    }

    }
}












// using Cysharp.Threading.Tasks;
// using UnityEngine;

// using System.Text;


// namespace GoveKits.Network
// {
//     public class NetSocketExample : MonoBehaviour
//     {
//         private NetSocket net;

//         async void Start()
//         {
//             // 注册消息（按你当前实现，MessageBuilder 存储 prototype）
//             MessageBuilder.AddMessage(1001, new PlayerMessage());

//             // 连接（回调示例）
//             net = new NetSocket("127.0.0.1", 12345,
//                 callbackSuccess: () => Debug.Log("Connected"),
//                 callbackFailure: () => Debug.LogWarning("Connect failed"));

//             // 如果想在 Start 中等待首次连接完成可用 await/timeout 逻辑（当前构造是异步的回调风格）
//             await UniTask.DelayFrame(1);
//         }

//         void Update()
//         {
//             // 每帧调度处理队列（不会阻塞主线程）
//             // Process() 在现有实现中返回 UniTask，所以这里用 .Forget() 快速触发
//             net?.Process().Forget();
//         }

//         // 一个示例发送函数（异步）
//         public async UniTaskVoid SendPlayerInfo(int id, string name)
//         {
//             if (net == null) return;

//             var msg = new PlayerMessage();
//             msg.MsgData = new PlayerData { PlayerId = id, Name = name };
//             // 发送并 await 完成（如果你不想 await，可使用 .Forget()）
//             await net.Send(msg);
//         }

//         // UI 按钮示例调用（非 async）
//         public void OnSendButtonClicked()
//         {
//             // 不等待发送完成
//             SendPlayerInfo(42, "Alice").Forget();
//         }

//         private void OnDestroy()
//         {
//             net?.Close();
//         }
//     }
// }





// namespace GoveKits.Network
// {
//     public class PlayerData : MessageData
//     {
//         public int PlayerId;
//         public string Name = "";

//         public override int ByteLength()
//         {
//             return sizeof(int) + sizeof(int) + Encoding.UTF8.GetByteCount(Name);
//         }

//         public override byte[] Writing()
//         {
//             int idx = 0;
//             byte[] bytes = new byte[ByteLength()];
//             WriteInt(bytes, PlayerId, ref idx);
//             WriteString(bytes, Name, ref idx);
//             return bytes;
//         }

//         public override int Reading(byte[] bytes, int beginIndex = 0)
//         {
//             int idx = beginIndex;
//             PlayerId = ReadInt(bytes, ref idx);
//             Name = ReadString(bytes, ref idx);
//             return idx - beginIndex;
//         }
//     }

//     public class PlayerMessage : Message
//     {
//         public PlayerMessage()
//         {
//             MsgID = 1001;
//             MsgData = new PlayerData();
//         }

//         private PlayerData Data => (PlayerData)MsgData;

//         // 处理收到的消息（异步接口）
//         public override UniTask Handle()
//         {
//             Debug.Log($"[PlayerMessage] Received PlayerId={Data.PlayerId}, Name={Data.Name}");
//             // 举例：收到后发回一个确认（可选）
//             return UniTask.CompletedTask;
//         }
//     }
// }