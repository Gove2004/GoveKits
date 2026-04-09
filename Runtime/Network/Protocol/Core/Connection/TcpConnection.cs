// === TcpConnection.cs ===
using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Network.Core
{
    public class TcpConnection : IConnection
    {
        public int ConnectionId { get; set; }
        public bool IsConnected => _socket != null && _socket.Connected;
        
        public event Action<int, int, byte[]> OnMessageReceived; // connId, msgId, payload
        public event Action<int> OnDisconnected;

        private Socket _socket;
        private byte[] _recvBuffer = new byte[64 * 1024];
        private int _bytesAvailable = 0;

        public TcpConnection(Socket socket, int connectionId)
        {
            _socket = socket;
            ConnectionId = connectionId;
            _socket.NoDelay = true; // 帧同步必备：关闭 Nagle 算法降低延迟
            ReceiveLoop().Forget();
        }

        public void Send(int msgId, byte[] payload)
        {
            if (!IsConnected) return;
            try
            {
                byte[] packet = MessageFramer.Pack(msgId, payload);
                _socket.Send(packet); // 简单粗暴的同步发送，如果要高并发可改异步
            }
            catch
            {
                Disconnect();
            }
        }

        private async UniTaskVoid ReceiveLoop()
        {
            while (IsConnected)
            {
                try
                {
                    // 把新数据拼接到剩余数据后面
                    int received = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(_recvBuffer, _bytesAvailable, _recvBuffer.Length - _bytesAvailable), 
                        SocketFlags.None);

                    if (received == 0) break; // 对方断开
                    _bytesAvailable += received;

                    // 循环拆包（解决粘包）
                    while (true)
                    {
                        try
                        {
                            int parsedBytes = MessageFramer.TryParse(_recvBuffer, _bytesAvailable, out int msgId, out byte[] payload);
                            if (parsedBytes == 0) break; // 长度不够，等下次

                            // 触发收到消息事件
                            OnMessageReceived?.Invoke(ConnectionId, msgId, payload);

                            // 把剩下的残包往前挪
                            _bytesAvailable -= parsedBytes;
                            if (_bytesAvailable > 0)
                            {
                                Buffer.BlockCopy(_recvBuffer, parsedBytes, _recvBuffer, 0, _bytesAvailable);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Packet Parse Error: {e.Message}");
                            Disconnect();
                            return;
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
            Disconnect();
        }

        public void Disconnect()
        {
            if (_socket != null)
            {
                try { _socket.Close(); } catch { }
                _socket = null;
                OnDisconnected?.Invoke(ConnectionId);
            }
        }
    }
}