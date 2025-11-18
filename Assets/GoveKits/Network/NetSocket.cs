using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
        // 使用非泛型基类存储工厂
        private static Dictionary<int, Func<Message>> factories = new();

        // 注册时使用具体类型
        public static void AddMessage<T>(int keyID, Message<T> prototype) where T : BinaryData, new()
        {
            if (prototype == null) throw new ArgumentNullException(nameof(prototype));
            factories[keyID] = () => (Message<T>)Activator.CreateInstance(prototype.GetType());
        }

        // 获取消息实例
        public static Message<T> GetMessage<T>(int keyID) where T : BinaryData, new()
        {
            if (factories.TryGetValue(keyID, out var f))
            {
                try { return (Message<T>)f(); }
                catch (Exception ex)
                {
                    Debug.LogError($"MessageBuilder.GetMessage factory for {keyID} threw: {ex}");
                    return null;
                }
            }
            Debug.LogWarning($"MessageBuilder: message id {keyID} not registered");
            return null;
        }

        // 非泛型版本，用于处理接收队列
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

    // 非泛型基类接口
    public abstract class Message : BinaryData
    {
        public abstract int MsgID { get; }
        public abstract UniTask Handle();
    }

    // 泛型消息类
    public abstract class Message<T> : Message where T : BinaryData, new()
    {
        // 消息ID
        public override int MsgID { get; }
        public T MsgData = new T();

        public Message(int msgID)
        {
            MsgID = msgID;
        }

        // 长度 => ID(4) + Length(4) + DataBody
        public override int Length() => sizeof(int) + sizeof(int) + MsgData.Length();

        // 序列化
        public override byte[] Writing()
        {
            int idx = 0;
            byte[] bytes = new byte[Length()];
            WriteInt(bytes, MsgID, ref idx);
            WriteInt(bytes, MsgData.Length(), ref idx);
            MsgData.Writing().CopyTo(bytes, idx);
            if (idx + MsgData.Length() != bytes.Length)
                Debug.LogError($"[Message] Writing length mismatch for MsgID={MsgID}: expected {bytes.Length}, wrote {idx + MsgData.Length()}");
            return bytes;
        }

        // 反序列化
        public override int Reading(byte[] buffer, int index)
        {  
            int startIndex = index;
            // 读取消息头
            int readMsgID = ReadInt(buffer, ref index);
            int dataLength = ReadInt(buffer, ref index);
            // 读取数据体
            index += MsgData.Reading(buffer, index);
            return index - startIndex;
        }


        public abstract override UniTask Handle();
    }

    public class NetSocket
    {
        private Socket socket;
        private byte[] cacheBytes = new byte[1024 * 1024];
        private int cacheNum = 0;

        // 使用非泛型Message队列
        private Queue<Message> receiveQueue = new Queue<Message>();
        private readonly object receiveLock = new object();
        private readonly SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        public float heartbeatInterval = 10f;

        public async UniTask Process()
        {
            try
            {
                if (socket == null) return;

                while (true)
                {
                    Message msg = null;
                    lock (receiveLock)
                    {
                        if (receiveQueue.Count > 0)
                            msg = receiveQueue.Dequeue();
                    }

                    if (msg == null) break;

                    try
                    {
                        await msg.Handle();
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

        public NetSocket(string host, int port, Action callbackSuccess = null, Action callbackFailure = null)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(host), port);
            args.RemoteEndPoint = ipPoint;
            args.Completed += (s, e) =>
            {
                if(e.SocketError == SocketError.Success)
                {
                    callbackSuccess?.Invoke();
                    Debug.Log($"[NetSocket] Connected to {host}:{port}");
                    
                    SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                    receiveArgs.SetBuffer(cacheBytes, 0, cacheBytes.Length);
                    receiveArgs.Completed += Receive;
                    socket.ReceiveAsync(receiveArgs);
                }
                else
                {
                    Debug.LogWarning($"[NetSocket] Connect failed: {e.SocketError}");
                    callbackFailure?.Invoke();
                }
            };
            socket.ConnectAsync(args);
        }

        // 发送泛型消息
        public async UniTask Send<T>(Message<T> msg) where T : BinaryData, new()
        {
            if (socket == null || !socket.Connected)
                throw new InvalidOperationException("Socket is null or closed");

            var data = msg.Writing();
            if (data == null || data.Length == 0) return;

            await sendSemaphore.WaitAsync();
            try
            {
                await UniTask.RunOnThreadPool(() =>
                {
                    int sent = 0;
                    while (sent < data.Length)
                    {
                        Debug.Log($"[NetSocket] Sending data chunk: {data.Length - sent} bytes");
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

        public void Receive(object obj, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success && args.BytesTransferred > 0)
            {
                HandleReceiveMsg(args.BytesTransferred);
                
                try
                {
                    args.SetBuffer(cacheNum, args.Buffer.Length - cacheNum);
                    if (socket != null && socket.Connected)
                        socket.ReceiveAsync(args);
                    else
                        Close();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetSocket] SetBuffer error: {ex}");
                    Close();
                }
            }
            else
            {
                Debug.LogWarning($"[NetSocket] Receive error: {args.SocketError}");
                Close();
            }
        }

        public void Close()
        {
            socket?.Close();
            socket = null;
        }

        private void HandleReceiveMsg(int receiveNum)
        {
            int msgID = 0;
            int msgLength = 0;
            int nowIndex = 0;

            cacheNum += receiveNum;
            
            while (nowIndex < cacheNum)
            {
                if (cacheNum - nowIndex >= 8)
                {
                    msgID = BitConverter.ToInt32(cacheBytes, nowIndex);  // 读取消息ID
                    nowIndex += 4;
                    msgLength = BitConverter.ToInt32(cacheBytes, nowIndex);  // 读取消息长度
                    nowIndex += 4;

                    if (msgLength >= 0 && cacheNum - nowIndex >= msgLength)
                    {
                        // 使用非泛型版本获取消息
                        Message msg = MessageBuilder.GetMessage(msgID);
                        if (msg != null)
                        {
                            try
                            {
                                int tempIndex = nowIndex - 8;  // 修正：回退到消息体开始位置
                                tempIndex += msg.Reading(cacheBytes, tempIndex);
                                
                                lock (receiveLock)
                                {
                                    receiveQueue.Enqueue(msg);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[NetSocket] Exception while reading msgID={msgID}: {ex}");
                            }
                        }

                        nowIndex += msgLength;
                    }
                    else
                    {
                        // 数据不完整，回退索引
                        Debug.LogWarning($"[NetSocket] Incomplete data for msgID={msgID}, expected length={msgLength}, available={cacheNum - nowIndex}");
                        nowIndex -= 8;
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[NetSocket] Incomplete header for msgID={msgID}, available bytes={cacheNum - nowIndex}");    
                    break;
                }
            }

            // 移动剩余数据
            if (nowIndex > 0 && nowIndex < cacheNum)
            {
                Array.Copy(cacheBytes, nowIndex, cacheBytes, 0, cacheNum - nowIndex);
                cacheNum -= nowIndex;
            }
            else if (nowIndex >= cacheNum)
            {
                cacheNum = 0;
            }
        }
    }
}






















// 使用示例：
namespace GoveKits.Network.Examples
{
    // 定义数据类
    public class PlayerData : BinaryData
    {
        public int PlayerId;
        public string Name = "";

        public override int Length()
        {
            // 第二个int为Name字符串的长度信息
            return sizeof(int) + (sizeof(int) + System.Text.Encoding.UTF8.GetByteCount(Name));
        }

        public override int Reading(byte[] buffer, int index)
        {
            int startIndex = index;
            PlayerId = ReadInt(buffer, ref index);
            Name = ReadString(buffer, ref index);
            return index - startIndex;
        }

        public override byte[] Writing()
        {
            int idx = 0;
            byte[] bytes = new byte[Length()];
            WriteInt(bytes, PlayerId, ref idx);
            WriteString(bytes, Name, ref idx);
            return bytes;
        }
    }

    // 定义消息类
    public class PlayerMessage : Message<PlayerData>
    {
        public PlayerMessage() : base(1001) { }

        public override async UniTask Handle()
        {
            Debug.Log($"[PlayerMessage] Received PlayerId={MsgData.PlayerId}, Name={MsgData.Name}");
            await UniTask.CompletedTask;
        }
    }

    // 使用示例
    public class NetworkExample 
    {
        private NetSocket netSocket;

        public void Initialize()
        {
            // 注册消息
            MessageBuilder.AddMessage(1001, new PlayerMessage());
            
            // 连接服务器
            netSocket = new NetSocket("127.0.0.1", 12345, 
                callbackSuccess: () => Debug.Log("Connected"),
                callbackFailure: () => Debug.Log("Connection failed"));
        }

        public async UniTask SendPlayerMessage(int playerId, string name)
        {
            var message = new PlayerMessage();
            message.MsgData.PlayerId = playerId;
            message.MsgData.Name = name;
            
            await netSocket.Send(message);
        }


        public void Update()
        {
            _ = netSocket.Process();
        }


        public void Shutdown()
        {
            netSocket.Close();
        }
    }
}