using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// UDP 通道实现。
    /// 使用 Socket 异步收发，无粘包问题（UDP 是消息协议）。
    /// </summary>
    public class UdpChannel : IChannel
    {
        private Socket _socket;
        private CancellationTokenSource _cts;
        private IPEndPoint _remoteEndpoint;

        /// <summary>
        /// 获取通道是否处于已连接状态。
        /// </summary>
        public bool IsConnected => _socket != null;

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
        /// 创建未绑定的 UDP 通道实例。
        /// </summary>
        public UdpChannel()
        {
            _socket = null;
        }

        /// <summary>
        /// 绑定本地端口。
        /// </summary>
        public void Bind(int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _cts = new CancellationTokenSource();
            StartReceiveLoop().Forget();
        }

        /// <summary>
        /// 连接到远程地址（UDP 无连接概念，仅记录目标地址）。
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ConnectAsync(EndPoint target)
        {
            try
            {
                if (_socket == null)
                {
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                    _cts = new CancellationTokenSource();
                    StartReceiveLoop().Forget();
                }
                _remoteEndpoint = (IPEndPoint)target;
                OnConnected?.Invoke(this);
                return true;
            }
            catch (Exception)
            {
                OnDisconnected?.Invoke(this, "连接失败");
                return false;
            }
        }

        /// <summary>
        /// 发送数据。UDP 无需长度前缀，直接发送。
        /// </summary>
        public void Send(byte[] data)
        {
            if (_socket == null || _remoteEndpoint == null) return;
            try
            {
                var saea = new SocketAsyncEventArgs();
                saea.RemoteEndPoint = _remoteEndpoint;
                saea.BufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(data) };
                saea.Completed += OnSendCompleted;
                if (!_socket.SendToAsync(saea))
                {
                    // Completed synchronously — no-op (handled by event)
                }
            }
            catch (Exception ex)
            {
                Close($"发送失败: {ex.Message}");
            }
        }

        private static void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnSendCompleted;
            if (e.SocketError != SocketError.Success)
            {
                var channel = sender as UdpChannel;
                channel?.Close($"发送错误: {e.SocketError}");
            }
        }

        private async UniTaskVoid StartReceiveLoop()
        {
            byte[] recvBuffer = new byte[4096];
            try
            {
                while (_cts != null && !_cts.Token.IsCancellationRequested)
                {
                    var result = await _socket.ReceiveFromAsync(recvBuffer, SocketFlags.None, _remoteEndpoint ?? new IPEndPoint(IPAddress.Any, 0));
                    int bytesRead = result.ReceivedBytes;

                    if (bytesRead == 0)
                    {
                        Close("远端断开连接");
                        break;
                    }

                    byte[] frameData = new byte[bytesRead];
                    Array.Copy(recvBuffer, frameData, bytesRead);
                    OnDataReceived?.Invoke(this, frameData);
                }
            }
            catch (Exception ex)
            {
                Close($"接收错误: {ex.Message}");
            }
        }

        public void Close(string reason = "")
        {
            if (_socket == null) return;
            try { _cts?.Cancel(); } catch { }
            try { _socket.Close(); } catch { }
            _socket = null;
            OnDisconnected?.Invoke(this, reason);
        }

        public void Dispose() => Close("已释放");
    }
}
