using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public class ByteSocket : IDisposable
    {
        private Socket _socket;
        private readonly byte[] _receiveBuffer = new byte[64 * 1024];
        
        private readonly Action<byte[], int, int> _onReceiveDataAction;  // 收到数据回调

        public bool IsConnected => _socket != null && _socket.Connected;

        public ByteSocket(Action<byte[], int, int> onReceiveData)
        {
            _onReceiveDataAction = onReceiveData;
        }

        public async UniTask ConnectAsync(string ip, int port)
        {
            Close();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) 
            { 
                NoDelay = true // 禁用 Nagle 算法，降低延迟
            };

            try
            {
                await _socket.ConnectAsync(ip, port);
                Debug.Log($"[NetSocket] Connected to {ip}:{port}");
                ReceiveLoopAsync().Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetSocket] Connect Failed: {ex.Message}");
                Close();
            }
        }

        private async UniTaskVoid ReceiveLoopAsync()
        {
            while (IsConnected)
            {
                try
                {
                    int received = await _socket.ReceiveAsync(new ArraySegment<byte>(_receiveBuffer), SocketFlags.None);
                    if (received == 0)
                    {
                        Close();
                        break;
                    }
                    // 直接推给 Parser
                    _onReceiveDataAction?.Invoke(_receiveBuffer, 0, received);
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
                await _socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
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
            try { _socket.Close(); } catch { }
            _socket = null;
        }

        public void Dispose() => Close();
    }
}