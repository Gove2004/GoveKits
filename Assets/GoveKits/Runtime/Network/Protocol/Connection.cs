
using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;





namespace GoveKits.Network
{
    // === 连接接口：负责数据传输 ===
    public interface IConnection : IDisposable
    {
        int Id { get; }
        bool IsConnected { get; }
        
        // 发送消息
        void Send(Message msg);
        
        // 主动关闭
        void Close();

        // 事件：收到消息 (Message, SenderID)
        event Action<Message, int> OnMessageReceived;
        
        // 事件：连接断开 (ConnectionID)
        event Action<int> OnDisconnected;
    }




    // === 1. 本地回环连接 (Host专用, 0延迟, 0GC) ===
    public class LocalConnection : IConnection
    {
        public int Id { get; private set; }
        public bool IsConnected => true;

        public event Action<Message, int> OnMessageReceived;
        public event Action<int> OnDisconnected;

        public LocalConnection(int id) => Id = id;

        public void Send(Message msg)
        {
            // 模拟异步防止死锁，直接传递引用（高性能）
            UniTask.Void(async () => 
            {
                await UniTask.Yield();
                // 确保 ID 正确 (防止篡改)
                msg.Header.SenderID = Id; 
                OnMessageReceived?.Invoke(msg, Id);
            });
        }

        public void Close()  // 本地连接无需关闭
        {
            OnDisconnected?.Invoke(Id);
        } 
        public void Dispose() { }
    }

    // === 2. TCP 网络连接 (通用) ===
    public class TcpConnection : IConnection
    {
        public int Id { get; private set; }
        public bool IsConnected => _socket != null && _socket.Connected;

        public event Action<Message, int> OnMessageReceived;
        public event Action<int> OnDisconnected;

        private Socket _socket;
        private readonly PacketParser _parser;
        private readonly byte[] _recvBuffer = new byte[64 * 1024];

        // 发送缓冲区锁，避免高频 new byte[]
        private readonly byte[] _sharedSendBuffer = new byte[64 * 1024];

        public TcpConnection(int id, Socket socket)
        {
            Id = id;
            _socket = socket;
            _socket.NoDelay = true;
            _parser = new PacketParser(OnParsedMsg);
            
            ReceiveLoopAsync().Forget();
        }

        private async UniTask OnParsedMsg(Message msg)
        {
            msg.Header.SenderID = this.Id; // 强制标记来源
            OnMessageReceived?.Invoke(msg, Id);
            await UniTask.CompletedTask;
        }

        private async UniTaskVoid ReceiveLoopAsync()
        {
            while (IsConnected)
            {
                try
                {
                    int len = await _socket.ReceiveAsync(new ArraySegment<byte>(_recvBuffer), SocketFlags.None);
                    if (len == 0) { Close(); break; }
                    _parser.InputRawData(_recvBuffer, 0, len);
                }
                catch { Close(); break; }
            }
        }

        public void Send(Message msg)
        {
            if (!IsConnected) return;

            byte[] dataToSend;
            
            // 序列化过程加锁
            lock (_sharedSendBuffer)
            {
                int index = 4; // 留4字节给长度头
                msg.Writing(_sharedSendBuffer, ref index);
                
                int bodyLen = index - 4;
                _sharedSendBuffer[0] = (byte)(bodyLen & 0xFF);
                _sharedSendBuffer[1] = (byte)((bodyLen >> 8) & 0xFF);
                _sharedSendBuffer[2] = (byte)((bodyLen >> 16) & 0xFF);
                _sharedSendBuffer[3] = (byte)((bodyLen >> 24) & 0xFF);

                // Copy 一次是必要的，因为 SendAsync 是异步的
                dataToSend = new byte[index];
                Array.Copy(_sharedSendBuffer, dataToSend, index);
            }

            try
            {
                _socket.SendAsync(new ArraySegment<byte>(dataToSend), SocketFlags.None).AsUniTask().Forget();
            }
            catch { Close(); }
        }

        public void Close()
        {
            if (_socket == null) return;
            try { _socket.Shutdown(SocketShutdown.Both); _socket.Close(); } catch { }
            _socket = null;
            OnDisconnected?.Invoke(Id); // 触发断开事件
            Debug.Log($"[Connection] {Id} Closed.");
        }

        public void Dispose() => Close();
    }
}