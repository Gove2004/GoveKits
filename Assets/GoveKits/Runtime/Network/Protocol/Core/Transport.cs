using System;
using System.Buffers;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network.Protocol
{
    public static class BufferPool
    {
        public static byte[] Rent(int size) => ArrayPool<byte>.Shared.Rent(size);
        public static void Return(byte[] buffer) => ArrayPool<byte>.Shared.Return(buffer);
    }

    public interface ITransport : IDisposable
    {
        bool IsConnected { get; }
        // 改为异步 Task，支持 await
        UniTask ConnectAsync(string ip, int port);
        void Send(byte[] data, int length);
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
        private readonly object _lockObj = new object(); // 线程锁

        public TcpTransport() { }

        public async UniTask ConnectAsync(string ip, int port)
        {
            Close(); 

            lock (_lockObj)
            {
                _cts = new System.Threading.CancellationTokenSource();
                _recvBuffer = BufferPool.Rent(BUFFER_SIZE);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true,
                    ReceiveBufferSize = BUFFER_SIZE,
                    SendBufferSize = BUFFER_SIZE
                };
            }

            try
            {
                // 原生异步连接 (5秒超时)
                await _socket.ConnectAsync(ip, port)
                             .AsUniTask()
                             .Timeout(TimeSpan.FromSeconds(5));

                // 连接成功，启动接收循环
                ReceiveLoop().Forget();
            }
            catch (Exception ex)
            {
                LogCore.LogError("Transport", $"Connect Failed: {ex.Message}");
                Close();
                throw; // 抛出异常给上层处理
            }
        }

        private async UniTaskVoid ReceiveLoop()
        {
            var token = _cts.Token;
            while (IsConnected && !token.IsCancellationRequested)
            {
                try
                {
                    int len = await _socket.ReceiveAsync(new ArraySegment<byte>(_recvBuffer), SocketFlags.None)
                                           .AsUniTask()
                                           .AttachExternalCancellation(token);

                    if (len == 0) { Close(); break; }
                    OnReceive?.Invoke(new ArraySegment<byte>(_recvBuffer, 0, len));
                }
                catch (OperationCanceledException) { break; }
                catch { Close(); break; }
            }
        }

        public void Send(byte[] data, int length)
        {
            if (!IsConnected) return;
            try
            {
                _socket.SendAsync(new ArraySegment<byte>(data, 0, length), SocketFlags.None)
                       .AsUniTask().AttachExternalCancellation(_cts.Token).Forget();
            }
            catch { Close(); }
        }

        public void Close()
        {
            lock (_lockObj)
            {
                if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch {} _cts = null; }
                
                if (_socket != null)
                {
                    try { if (_socket.Connected) _socket.Shutdown(SocketShutdown.Both); } catch {}
                    try { _socket.Close(); } catch {}
                    _socket = null;
                    
                    // 安全触发回调
                    try { OnDisconnected?.Invoke(); } 
                    catch (Exception e) { LogCore.LogError("Transport", e.ToString()); }
                }

                if (_recvBuffer != null)
                {
                    BufferPool.Return(_recvBuffer);
                    _recvBuffer = null;
                }
            }
        }

        public void Dispose() => Close();
    }
}