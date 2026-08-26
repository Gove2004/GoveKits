using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// TCP 通道实现。
    /// 使用 Socket 异步收发，Nagle 算法关闭以保证低延迟。
    /// 内部持有 DataSplitter 处理粘包/半包。
    /// </summary>
    public class TcpChannel : IChannel
    {
        private Socket _socket;
        private readonly DataSplitter _splitter;
        private CancellationTokenSource _cts;

        /// <summary>
        /// 获取通道是否处于已连接状态。
        /// </summary>
        public bool IsConnected => _socket?.Connected ?? false;

        /// <summary>
        /// 获取远程端点的网络地址信息。
        /// </summary>
        public EndPoint RemoteEndPoint => _socket?.RemoteEndPoint;

        /// <summary>
        /// 成功建立连接时触发。
        /// </summary>
        public event Action<IChannel> OnConnected;

        /// <summary>
        /// 连接断开时触发，参数为断开原因。
        /// </summary>
        public event Action<IChannel, string> OnDisconnected;

        /// <summary>
        /// 收到数据帧时触发，参数为原始字节数据。
        /// </summary>
        public event Action<IChannel, byte[]> OnDataReceived;

        /// <summary>
        /// 创建未绑定的 TCP 通道实例，需手动调用 ConnectAsync 进行连接。
        /// </summary>
        public TcpChannel()
        {
            _socket = null;
            _splitter = new DataSplitter();
        }

        /// <summary>
        /// 使用已接受的 Socket 创建 TCP 通道实例，自动启动接收循环。
        /// </summary>
        /// <param name="acceptedSocket">服务端 Accept 获得的 Socket。</param>
        public TcpChannel(Socket acceptedSocket) : this()
        {
            _socket = acceptedSocket;
            _socket.NoDelay = true;
            _cts = new CancellationTokenSource();
            StartReceiveLoop().Forget();
        }

        public async System.Threading.Tasks.Task<bool> ConnectAsync(EndPoint target)
        {
            try
            {
                Close("正在重连");
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
                OnDisconnected?.Invoke(this, "连接失败");
                return false;
            }
        }

        /// <summary>
        /// 向远端发送字节数据，自动添加长度前缀以处理粘包/半包问题。
        /// </summary>
        /// <param name="data">要发送的字节数组。</param>
        public void Send(byte[] data)
        {
            Socket socket;
            if (_socket == null || !IsConnected) return;
            socket = _socket;
            try
            {
                byte[] sendBuffer = new byte[4 + data.Length];
                BinaryPrimitives.WriteInt32LittleEndian(sendBuffer.AsSpan(0, 4), data.Length);
                Buffer.BlockCopy(data, 0, sendBuffer, 4, data.Length);

                int total = sendBuffer.Length;
                int offset = 0;
                while (offset < total)
                {
                    int sent = socket.Send(sendBuffer, offset, total - offset, SocketFlags.None);
                    if (sent == 0) { Close("远端已关闭"); return; }
                    offset += sent;
                }
            }
            catch (Exception ex)
            {
                Close($"发送失败: {ex.Message}");
            }
        }

        private async UniTaskVoid StartReceiveLoop()
        {
            byte[] recvBuffer = new byte[65536];
            try
            {
                while (IsConnected && _cts != null && !_cts.Token.IsCancellationRequested)
                {
                    int bytesRead = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(recvBuffer), SocketFlags.None);

                    if (bytesRead == 0)
                    {
                        Close("远端断开连接");
                        break;
                    }

                    _splitter.Feed(new ArraySegment<byte>(recvBuffer, 0, bytesRead));

                    while (_splitter.TryExtract(out byte[] frameData))
                    {
                        OnDataReceived?.Invoke(this, frameData);
                    }
                }
            }
            catch (Exception ex)
            {
                Close($"接收错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭 TCP 连接并触发断开事件。
        /// </summary>
        /// <param name="reason">断开原因说明。</param>
        public void Close(string reason = "")
        {
            Socket deadSocket;
            if (_socket == null) return;
            deadSocket = _socket;
            _socket = null;
            try { _cts?.Cancel(); } catch { }
            try { deadSocket.Close(); } catch { }
            deadSocket.Dispose();
            _splitter.Clear();
            OnDisconnected?.Invoke(this, reason);
        }

        /// <summary>
        /// 释放 TCP 通道占用的所有资源，等价于 Close("已释放")。
        /// </summary>
        public void Dispose() => Close("已释放");
    }
}
