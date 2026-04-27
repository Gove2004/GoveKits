using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public class TcpConnection : IConnection
    {
        public bool IsConnected => _socket?.Connected ?? false;
        public EndPoint RemoteEndPoint => _socket?.RemoteEndPoint;

        public event Action<IConnection> OnConnected;
        public event Action<IConnection, string> OnDisconnected;
        public event Action<IConnection, byte[]> OnDataReceived;

        private Socket _socket;
        private CancellationTokenSource _cts;
        private readonly DataSplitter _splitter; // 持有拆包器

        public TcpConnection()
        {
            _splitter = new DataSplitter();
        }

        // 服务端 Accept 产生的 Socket
        public TcpConnection(Socket acceptedSocket) : this()
        {
            _socket = acceptedSocket;
            _socket.NoDelay = true;
            _cts = new CancellationTokenSource();
            StartReceiveLoop().Forget();
        }

        // 客户端主动 Connect
        public async Task<bool> ConnectAsync(EndPoint target)
        {
            try
            {
                Close("Reconnecting");
                _socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true;
                _cts = new CancellationTokenSource();
                _splitter.Clear();

                await _socket.ConnectAsync(target);
                await UniTask.SwitchToMainThread();
                OnConnected?.Invoke(this);
                StartReceiveLoop().Forget();
                return true;
            }
            catch (Exception)
            {
                OnDisconnected?.Invoke(this, "Connect failed");
                return false;
            }
        }

        public void Send(byte[] data)
        {
            if (!IsConnected) return;
            try
            {
                // 发送时自动盖上 4 字节长度头
                byte[] sendBuffer = new byte[4 + data.Length];
                BinaryPrimitives.WriteInt32LittleEndian(sendBuffer.AsSpan(0, 4), data.Length);
                Buffer.BlockCopy(data, 0, sendBuffer, 4, data.Length);

                _socket.Send(sendBuffer, 0, sendBuffer.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Close($"Send failed: {ex.Message}");
            }
        }

        private async UniTaskVoid StartReceiveLoop()
        {
            byte[] recvBuffer = new byte[4096];
            try
            {
                while (IsConnected && !_cts.IsCancellationRequested)
                {
                    // 异步读取任意长度的网络切片
                    int bytesRead = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(recvBuffer), SocketFlags.None);

                    if (bytesRead == 0)
                    {
                        Close("Remote disconnected");
                        break;
                    }

                    // 喂给拆包器
                    _splitter.Feed(new ArraySegment<byte>(recvBuffer, 0, bytesRead));

                    // 循环提取出所有完整的业务包
                    while (_splitter.TryExtract(out byte[] frameData))
                    {
                        OnDataReceived?.Invoke(this, frameData);
                    }
                }
            }
            catch (Exception ex)
            {
                Close($"Receive error: {ex.Message}");
            }
        }

        public void Close(string reason = "")
        {
            if (_socket == null) return;
            try { _cts?.Cancel(); } catch { }
            try { _socket.Close(); } catch { }
            _socket = null;
            _splitter.Clear();
            OnDisconnected?.Invoke(this, reason);
        }

        public void Dispose() => Close("Dispose");
    }
}