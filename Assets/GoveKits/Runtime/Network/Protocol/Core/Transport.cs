using System;
using System.Buffers;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    // 简易内存池工具，避免反复 new byte[]
    public static class BufferPool
    {
        public static byte[] Rent(int size) => ArrayPool<byte>.Shared.Rent(size);
        public static void Return(byte[] buffer) => ArrayPool<byte>.Shared.Return(buffer);
    }

    public interface ITransport : IDisposable
    {
        bool IsConnected { get; }
        void Connect(string ip, int port);
        // 支持发送长度，避免发送整个数组
        void Send(byte[] data, int length);
        // 使用 ArraySegment 实现零拷贝接收
        Action<ArraySegment<byte>> OnReceive { get; set; } 
        Action OnDisconnected { get; set; }
        void Close();
    }


    public class TcpTransport : ITransport
    {
        public bool IsConnected => _socket != null && _socket.Connected;
        public Action<ArraySegment<byte>> OnReceive { get; set; }
        public Action OnDisconnected { get; set; }

        private Socket _socket;
        private byte[] _recvBuffer;
        private const int BUFFER_SIZE = 64 * 1024;
        private System.Threading.CancellationTokenSource _cts;

        // 无参构造，初始化时不创建 Socket
        public TcpTransport() { }

        public void Connect(string ip, int port)
        {
            Close(); // 清理旧连接
            
            _cts = new System.Threading.CancellationTokenSource();
            _recvBuffer = BufferPool.Rent(BUFFER_SIZE); 
            
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveBufferSize = BUFFER_SIZE,
                SendBufferSize = BUFFER_SIZE
            };

            ConnectAsync(ip, port).Forget();
        }

        private async UniTaskVoid ConnectAsync(string ip, int port)
        {
            try
            {
                // 设置 5秒 超时
                await _socket.ConnectAsync(ip, port)
                             .AsUniTask()
                             .AttachExternalCancellation(_cts.Token)
                             .Timeout(TimeSpan.FromSeconds(5));

                // 连接成功后才启动接收循环
                ReceiveLoop().Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Transport] Connect Failed: {ex.Message}");
                Close();
            }
        }

        private async UniTaskVoid ReceiveLoop()
        {
            var token = _cts.Token;
            while (IsConnected && !token.IsCancellationRequested)
            {
                try
                {
                    // 直接读取到池化数组中
                    int len = await _socket.ReceiveAsync(new ArraySegment<byte>(_recvBuffer), SocketFlags.None)
                                           .AsUniTask()
                                           .AttachExternalCancellation(token);
                    
                    if (len == 0) { Close(); break; } 

                    // 抛出切片数据
                    OnReceive?.Invoke(new ArraySegment<byte>(_recvBuffer, 0, len));
                }
                catch { Close(); break; }
            }
        }

        public void Send(byte[] data, int length)
        {
            if (!IsConnected) return;
            try
            {
                // 发送指定长度的数据
                _socket.SendAsync(new ArraySegment<byte>(data, 0, length), SocketFlags.None)
                       .AsUniTask().AttachExternalCancellation(_cts.Token).Forget();
            }
            catch { Close(); }
        }

        public void Close()
        {
            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); _cts = null; }

            if (_socket != null)
            {
                try { if (_socket.Connected) _socket.Shutdown(SocketShutdown.Both); _socket.Close(); } catch { }
                _socket = null;
                OnDisconnected?.Invoke();
            }

            if (_recvBuffer != null)
            {
                BufferPool.Return(_recvBuffer);
                _recvBuffer = null;
            }
        }

        public void Dispose() => Close();
    }
}