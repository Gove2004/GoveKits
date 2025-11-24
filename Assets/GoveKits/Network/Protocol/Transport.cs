

using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    // === 传输层接口：负责建立连接 ===
    public interface ITransport
    {
        // 启动服务器监听
        // onClientConnected: 当有新客户端连入时触发，返回建立好的连接对象
        void StartServer(int port, Action<IConnection> onClientConnected);

        // 连接到服务器
        // onConnected: 连接成功触发
        // onFailure: 连接失败触发
        void ConnectClient(string ip, int port, Action<IConnection> onConnected, Action onFailure);

        void Shutdown();
    }


    public class TcpTransport : ITransport
    {
        private Socket _listener;
        private bool _isRunning;

        public void StartServer(int port, Action<IConnection> onClientConnected)
        {
            if (_isRunning) return;
            _isRunning = true;
            
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(new IPEndPoint(IPAddress.Any, port));
                _listener.Listen(NetworkManager.MaxConnections);
                AcceptLoop(onClientConnected).Forget();
                Debug.Log($"[TcpTransport] Listening on {port}...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TcpTransport] Start Error: {ex}");
                Shutdown();
            }
        }

        private async UniTaskVoid AcceptLoop(Action<IConnection> onConnected)
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    var clientSocket = await _listener.AcceptAsync();
                    // 创建新连接并回调
                    var conn = new TcpConnection(NetworkManager.NextPlayerID++, clientSocket);
                    onConnected?.Invoke(conn);
                }
                catch { break; }
            }
        }

        public async void ConnectClient(string ip, int port, Action<IConnection> onConnected, Action onFailure)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(ip, port);
                var conn = new TcpConnection(NetworkManager.ClientTempID, socket);  // 等待服务器分配
                onConnected?.Invoke(conn);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TcpTransport] Connect Error: {ex}");
                onFailure?.Invoke();
            }
        }

        public void Shutdown()
        {
            _isRunning = false;
            _listener?.Close();
            _listener = null;
        }
    }






}