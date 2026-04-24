using System;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ClientCore
    {
        public static int PlayerId { get; private set; }
        public static float RTT { get; private set; }
        public static bool IsConnected => _connection?.IsConnected ?? false;
        
        public static event Action OnConnected;
        public static event Action<string> OnDisconnected;

        private static IConnection _connection;
        
        // 框架内置消息代理
        private static readonly ClientNetworkProxy _proxy = new ClientNetworkProxy();

        public static async UniTask ConnectAsync(string host, int port)
        {
            Shutdown();
            
            // 绑定内置 Proxy 接管底层协议
            DispatcherCore.Bind(_proxy);
            
            try
            {
                _connection = new TcpConnection();
                _connection.OnFrameReceived += OnFrameReceived;
                _connection.OnDisconnected += (conn, reason) => 
                {
                    PlayerId = 0;
                    OnDisconnected?.Invoke(reason);
                };
                
                // 建立连接后，立刻向服务端发送 Hello 握手
                _connection.OnConnected += (conn) => 
                {
                    Send(new HelloServerMsg());
                    OnConnected?.Invoke();
                };

                IPAddress targetIP;
                if (!IPAddress.TryParse(host, out targetIP))
                {
                    var ips = await Dns.GetHostAddressesAsync(host);
                    targetIP = Array.Find(ips, ip => ip.AddressFamily == AddressFamily.InterNetwork);
                    if (targetIP == null) targetIP = ips[0]; 
                }

                bool success = await _connection.ConnectAsync(new IPEndPoint(targetIP, port));
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
            if (!IsConnected) return;
            
            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return;

            byte[] purePayload = ProtocolCore.Serialize(message);
            byte[] frameData = new byte[2 + purePayload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(frameData, id);
            Buffer.BlockCopy(purePayload, 0, frameData, 2, purePayload.Length);

            _connection.SendFrame(frameData);
        }

        private static void OnFrameReceived(IConnection conn, byte[] frameData)
        {
            try
            {
                if (frameData.Length < 2) return;
                ushort protocolId = BinaryPrimitives.ReadUInt16LittleEndian(frameData);
                var purePayload = new ReadOnlyMemory<byte>(frameData, 2, frameData.Length - 2);

                var msg = ProtocolCore.Deserialize(protocolId, purePayload);
                if (msg != null)
                {
                    // 客户端 Dispatch 传 null
                    DispatcherCore.DispatchAsync(null, protocolId, msg).Forget();
                }
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(ClientCore), $"处理消息失败: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            _connection?.Close("Client Shutdown");
            _connection = null;
            PlayerId = 0;
            DispatcherCore.Unbind(_proxy);
        }

        // =========================================================
        // 内置消息代理：处理底层握手与心跳
        // =========================================================
        private class ClientNetworkProxy
        {
            [MessageHandler]
            public void OnHello(HelloClientMsg msg)
            {
                if (PlayerId != 0) return;
                PlayerId = msg.Id;
                LogCore.Info(nameof(ClientNetworkProxy), $"握手成功，服务器分配的 PlayerId: {PlayerId}, 寄语: {msg.Msg}");

                Send(new PingMsg() { ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            }

            [MessageHandler]
            public void OnBye(ByebyeClientMsg msg)
            {
                LogCore.Info(nameof(ClientNetworkProxy), $"服务器踢出：收到 Bye 消息");
                Shutdown();
            }

            [MessageHandler]
            public void OnPong(PongMsg msg)
            {
                RTT = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - msg.ClientTimestamp;
                // LogCore.Debug(nameof(ClientNetworkProxy), $"收到 Pong，RTT = {RTT} ms");

                // 每隔 5 秒发送一次心跳包
                UniTask.Delay(5000).ContinueWith(() =>
                    Send(new PingMsg() {
                        ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        ServerTimestamp = msg.ServerTimestamp
                    })
                );
            }
        }
    }
}