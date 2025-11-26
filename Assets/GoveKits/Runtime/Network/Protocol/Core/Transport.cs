using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;

namespace GoveKits.Network
{
    // === 传输层接口 ===
    public interface ITransport : IDisposable
    {
        bool IsConnected { get; }
        void Send(byte[] data);
        Action<byte[]> OnReceiveData { get; set; }
        Action OnDisconnected { get; set; }
        void Close();
    }


    
    // TCP 实现
    public class TcpSocketTransport : ITransport
    {
        private Socket _socket;
        public bool IsConnected => _socket != null && _socket.Connected;
        public Action<byte[]> OnReceiveData { get; set; }
        public Action OnDisconnected { get; set; }

        private readonly byte[] _recvBuffer = new byte[64 * 1024];

        public TcpSocketTransport(Socket socket)
        {
            _socket = socket;
            if(_socket != null) _socket.NoDelay = true;
            ReceiveLoopAsync().Forget();
        }

        private async UniTaskVoid ReceiveLoopAsync()
        {
            while (IsConnected)
            {
                try
                {
                    int len = await _socket.ReceiveAsync(new ArraySegment<byte>(_recvBuffer), SocketFlags.None);
                    if (len == 0) { Close(); break; }
                    
                    byte[] received = new byte[len];
                    Array.Copy(_recvBuffer, received, len);
                    OnReceiveData?.Invoke(received);
                }
                catch { Close(); break; }
            }
        }

        public void Send(byte[] data)
        {
            if (!IsConnected) return;
            try
            {
                _socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None).AsUniTask().Forget();
            }
            catch { Close(); }
        }

        public void Close()
        {
            if (_socket == null) return;
            try { _socket.Shutdown(SocketShutdown.Both); _socket.Close(); } catch { }
            _socket = null;
            OnDisconnected?.Invoke();
        }
        public void Dispose() => Close();
    }

    // Host 模式本地管道
    public class LocalTransport : ITransport
    {
        public bool IsConnected => true; // 始终连接
        public Action<byte[]> OnReceiveData { get; set; }
        public Action OnDisconnected { get; set; }

        private LocalTransport _target;

        public void ConnectTo(LocalTransport other)
        {
            _target = other;
            other._target = this;
        }

        public void Send(byte[] data)
        {
            // 模拟 1帧 网络延迟，防止死锁并模拟真实环境
            byte[] copy = new byte[data.Length];
            Array.Copy(data, copy, data.Length);
            
            UniTask.Void(async () => {
                await UniTask.Yield(); 
                _target?.OnReceiveData?.Invoke(copy);
            });
        }

        public void Close() => OnDisconnected?.Invoke();
        public void Dispose() { }
    }
}