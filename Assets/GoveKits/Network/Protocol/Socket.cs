using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetSocket : IDisposable
    {
        private Socket _socket;
        private readonly byte[] _receiveBuffer = new byte[64 * 1024];
        
        // 事件：原始数据 (buffer, offset, count)
        public event Action<byte[], int, int> OnReceiveData;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public bool IsConnected => _socket != null && _socket.Connected;

        public async UniTask ConnectAsync(string ip, int port)
        {
            Close();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            
            try
            {
                await _socket.ConnectAsync(ip, port);
                OnConnected?.Invoke();
                ReceiveLoopAsync().Forget(); // 开始接收循环
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetSocket] Connect failed: {ex.Message}");
                Close();
            }
        }

        private async UniTaskVoid ReceiveLoopAsync()
        {
            while (IsConnected)
            {
                try
                {
                    // 使用 UniTask 的 Socket 扩展或原生 ReceiveAsync
                    int received = await _socket.ReceiveAsync(new ArraySegment<byte>(_receiveBuffer), SocketFlags.None);
                    
                    if (received == 0)
                    {
                        Close(); // 收到0字节表示断开
                        break;
                    }

                    // ★ 收到数据直接抛出，不做任何逻辑处理
                    OnReceiveData?.Invoke(_receiveBuffer, 0, received);
                }
                catch (Exception)
                {
                    Close();
                    break;
                }
            }
        }
        
        public async UniTask SendAsync(byte[] data)
        {
            if (!IsConnected) return;
            try
            {
                int sent = 0;
                while (sent < data.Length)
                {
                    var segment = new ArraySegment<byte>(data, sent, data.Length - sent);
                    int s = await _socket.SendAsync(segment, SocketFlags.None);
                    if (s == 0) break;
                    sent += s;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetSocket] Send Error: {ex.Message}");
                Close();
            }
        }
        public void Close() 
        {
            if (_socket == null) return;
            try { _socket.Close(); } catch {}
            _socket = null;
            OnDisconnected?.Invoke();
        }
        public void Dispose() => Close();
    }
}