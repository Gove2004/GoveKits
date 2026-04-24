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
        public event Action<IConnection, byte[]> OnFrameReceived;

        private Socket _socket;
        private CancellationTokenSource _cts;

        public TcpConnection() { }

        public TcpConnection(Socket acceptedSocket)
        {
            _socket = acceptedSocket;
            _socket.NoDelay = true;
            _cts = new CancellationTokenSource();
            StartReceiveLoop().Forget();
        }

        public async Task<bool> ConnectAsync(EndPoint target)
        {
            try
            {
                Close("Reconnecting");
                _socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true;
                _cts = new CancellationTokenSource();

                await _socket.ConnectAsync(target);
                OnConnected?.Invoke(this);
                StartReceiveLoop().Forget();
                return true;
            }
            catch (Exception ex)
            {
                OnDisconnected?.Invoke(this, $"Connect failed: {ex.Message}");
                return false;
            }
        }

        public void SendFrame(byte[] frameData)
        {
            if (!IsConnected) return;
            try
            {
                // 自动装配 [4字节长度头]
                byte[] sendBuffer = new byte[4 + frameData.Length];
                BinaryPrimitives.WriteInt32LittleEndian(sendBuffer.AsSpan(0, 4), frameData.Length);
                Buffer.BlockCopy(frameData, 0, sendBuffer, 4, frameData.Length);

                _socket.Send(sendBuffer, 0, sendBuffer.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Close($"Send failed: {ex.Message}");
            }
        }

        private async UniTaskVoid StartReceiveLoop()
        {
            try
            {
                byte[] lengthBuffer = new byte[4];

                while (IsConnected && !_cts.IsCancellationRequested)
                {
                    // 1. 精确读取 4 字节长度头
                    if (!await ReadExactAsync(lengthBuffer, 4)) break;
                    int frameLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

                    if (frameLength <= 0 || frameLength > 1024 * 1024 * 5) // 5MB保护
                    {
                        Close("Invalid frame length");
                        break;
                    }

                    // 2. 根据长度，精确读取完整数据帧
                    byte[] frameData = new byte[frameLength];
                    if (!await ReadExactAsync(frameData, frameLength)) break;

                    // 3. 抛出完整包
                    OnFrameReceived?.Invoke(this, frameData);
                }
            }
            catch (Exception ex)
            {
                Close($"Receive error: {ex.Message}");
            }
        }

        // 核心读取工具：必须读满指定字节数，解决 TCP 碎片化问题
        private async Task<bool> ReadExactAsync(byte[] buffer, int requiredBytes)
        {
            int offset = 0;
            while (offset < requiredBytes)
            {
                int read = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer, offset, requiredBytes - offset), 
                    SocketFlags.None);
                
                if (read == 0) return false; // 远端断开
                offset += read;
            }
            return true;
        }

        public void Close(string reason = "")
        {
            if (_socket == null) return;
            try { _cts?.Cancel(); } catch { }
            try { _socket.Close(); } catch { }
            _socket = null;
            OnDisconnected?.Invoke(this, reason);
        }

        public void Dispose() => Close("Dispose");
    }
}