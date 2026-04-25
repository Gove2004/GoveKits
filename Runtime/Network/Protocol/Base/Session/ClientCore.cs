using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ClientCore
    {
        public static int PlayerId { get; private set; }
        public static float RTT { get; private set; }
        public static bool IsConnected => _session?.IsConnected ?? false;
        public static bool IsHost => ServerCore.IsListening;
        
        public static event Action OnConnected;
        public static event Action<string> OnDisconnected;

        // 对外暴露静态的 Dispatcher，方便其他组件注册
        public static MessageDispatcher Dispatcher { get; private set; } = new MessageDispatcher();
        
        // 客户端唯一的会话实例
        private static Session _session;
        
        // 框架内置消息代理
        private static readonly ClientNetworkProxy _proxy = new ClientNetworkProxy();

        public static async UniTask ConnectAsync(string host, int port)
        {
            Shutdown();
            
            // 绑定内置 Proxy 接管底层协议
            Dispatcher.Bind(_proxy);
            
            try
            {
                var connection = new TcpConnection();
                
                // 连接成功后，通过 Session 发送握手
                connection.OnConnected += (conn) => 
                {
                    _session.Send(new HelloServerMsg());
                    OnConnected?.Invoke();
                };

                // 创建客户端会话实例 (客户端本地视角的 SessionId 暂定为 0)
                _session = new Session(0, connection, Dispatcher);
                _session.OnClosed += (session, reason) => 
                {
                    PlayerId = 0;
                    OnDisconnected?.Invoke(reason);
                };

                IPAddress targetIP;
                if (!IPAddress.TryParse(host, out targetIP))
                {
                    var ips = await Dns.GetHostAddressesAsync(host);
                    targetIP = Array.Find(ips, ip => ip.AddressFamily == AddressFamily.InterNetwork) ?? ips[0];
                }

                bool success = await connection.ConnectAsync(new IPEndPoint(targetIP, port));
                if (!success) throw new Exception("Connection refused.");
                
                LogCore.Success(nameof(ClientCore), $"已连接 {host}:{port}");
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(ClientCore), $"连接失败: {ex.Message}");
                OnDisconnected?.Invoke($"Connect failed: {ex.Message}");
            }
        }

        public static void Send<T>(T message) where T : IProtocolMessage
        {
            _session?.Send(message);
        }

        public static void Shutdown()
        {
            _session?.Kick("Client Shutdown");
            _session = null;
            PlayerId = 0;
            Dispatcher.Unbind(_proxy);
        }

        // =========================================================
        // 内置消息代理：处理底层握手与心跳 (注意：客户端签名只有一个参数 Msg)
        // =========================================================
        private class ClientNetworkProxy
        {
            [MessageHandler]
            public void OnHello(HelloClientMsg msg)
            {
                if (PlayerId != 0) return;
                PlayerId = msg.Id;
                LogCore.Debug(nameof(ClientCore), $"握手成功，服务器分配的 PlayerId: {PlayerId}, 寄语: {msg.Msg}");

                Send(new PingMsg() { ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            }

            [MessageHandler]
            public void OnBye(ByebyeClientMsg msg)
            {
                LogCore.Debug(nameof(ClientCore), $"服务器踢出：收到 Bye 消息");
                Shutdown();
            }

            [MessageHandler]
            public void OnPong(PongMsg msg)
            {
                RTT = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - msg.ClientTimestamp;
                
                // 收到 Pong 后，延迟 5 秒继续发 Ping，维持心跳
                UniTask.Delay(5000).ContinueWith(() =>
                {
                    if (IsConnected)
                    {
                        Send(new PingMsg() {
                            ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            ServerTimestamp = msg.ServerTimestamp
                        });
                    }
                }).Forget();
            }
        }
    }
}