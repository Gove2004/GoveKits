using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ClientCore
    {
        private static ProtocolCenter _protocol;
        private static MessageDispatcher _dispatcher;
        private static ClientNetworkProxy _proxy;
        private static ClientInputor _clientInputor;
        private static ClientLerper _clientLerper;

        private static Session _session;
        private static int _playerId;
        private static float _rtt;
        internal static CancellationTokenSource _pingCts;

        public static ProtocolCenter Protocol => _protocol;
        public static MessageDispatcher Dispatcher => _dispatcher;
        public static bool IsConnected => _session?.IsConnected ?? false;
        /// <summary>
        /// 当前玩家 ID，由服务器在握手阶段分配。
        /// </summary>
        public static int PlayerId => _playerId;

        /// <summary>
        /// 当前往返延迟（RTT），单位为毫秒，由心跳机制维护。
        /// </summary>
        public static float RTT => _rtt;

        /// <summary>
        /// 客户端成功连接到服务器时触发。
        /// </summary>
        public static event Action OnConnected;

        /// <summary>
        /// 客户端与服务器断开连接时触发，参数为断开原因。
        /// </summary>
        public static event Action<string> OnDisconnected;

        /// <summary>
        /// 客户端提交输入数据时触发，参数为输入类型和输入数据对象。
        /// </summary>
        public static event Action<Type, object> OnInputSubmit;

        /// <summary>
        /// 初始化客户端核心。
        /// </summary>
        public static void Setup()
        {
            _clientInputor = new ClientInputor();
            _clientLerper = new ClientLerper(0.05f);
            _protocol = new ProtocolCenter();
            _dispatcher = new MessageDispatcher(_protocol);
            _proxy = new ClientNetworkProxy();
            _clientInputor.OnSubmit += (type, data) => OnInputSubmit?.Invoke(type, data);
        }

        public static async UniTask ConnectAsync(string host, int port)
        {
            Shutdown();
            _dispatcher.Bind(_proxy);

            try
            {
                var connection = new TcpChannel();
                connection.OnConnected += (conn) =>
                {
                    _session.Send(new HelloServerMsg());
                    OnConnected?.Invoke();
                };

                _session = new Session(0, connection, _dispatcher, _protocol);
                _session.OnClosed += (session, reason) =>
                {
                    _playerId = 0;
                    OnDisconnected?.Invoke(reason);
                };

                IPAddress targetIP;
                if (!IPAddress.TryParse(host, out targetIP))
                {
                    var ips = await Dns.GetHostAddressesAsync(host);
                    targetIP = Array.Find(ips, ip => ip.AddressFamily == AddressFamily.InterNetwork) ?? ips[0];
                }

                bool success = await connection.ConnectAsync(new IPEndPoint(targetIP, port));
                if (!success) throw new Exception("连接被拒绝。");

                try { LogCore.Success(nameof(ClientCore), $"已连接 {host}:{port}"); } catch { }
            }
            catch (Exception ex)
            {
                try { LogCore.Error(nameof(ClientCore), $"连接失败: {ex.Message}"); } catch { }
                OnDisconnected?.Invoke($"连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 向服务器发送协议消息。
        /// </summary>
        /// <typeparam name="T">消息类型，必须实现 IProtocolMessage。</typeparam>
        /// <param name="message">要发送的消息实例。</param>
        public static void Send<T>(T message) where T : IProtocolMessage => _session?.Send(message);

        /// <summary>
        /// 修改当前待提交的输入数据，用于在输入提交前注入或调整字段。
        /// </summary>
        /// <typeparam name="T">输入数据类型。</typeparam>
        /// <param name="modifier">输入数据的修改回调。</param>
        public static void ModifyInput<T>(Action<T> modifier) where T : new() => _clientInputor.ModifyInput(modifier);

        /// <summary>
        /// 驱动客户端输入器和插值器更新。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        public static void Update(float deltaTime) { _clientInputor?.Update(deltaTime); _clientLerper?.Update(deltaTime); }

        /// <summary>
        /// 启动客户端帧同步系统，初始化输入器和插值器。
        /// </summary>
        /// <param name="submitInterval">输入提交间隔。</param>
        /// <param name="serverTickRate">服务端 Tick 间隔。</param>
        public static void StartClient(float submitInterval = 0.05f, float serverTickRate = 0.05f)
        {
            _clientInputor = new ClientInputor(submitInterval);
            _clientLerper = new ClientLerper(serverTickRate);
            _clientInputor.OnSubmit += (type, data) => OnInputSubmit?.Invoke(type, data);
        }

        public static void Shutdown()
        {
            _pingCts?.Cancel();
            _pingCts?.Dispose();
            _pingCts = null;

            _session?.Kick("客户端关闭");
            _session = null;
            _playerId = 0;
            _dispatcher.Unbind(_proxy);
        }

        public static void Close() => Shutdown();

        /// <summary>
        /// 设置当前玩家 ID（由握手消息触发）。
        /// </summary>
        internal static void SetPlayerId(int id) => _playerId = id;

        /// <summary>
        /// 设置当前往返延迟（由心跳响应触发）。
        /// </summary>
        internal static void SetRTT(float rtt) => _rtt = rtt;
    }

    /// <summary>
    /// 客户端网络代理，处理握手、心跳和断线等协议消息。
    /// </summary>
    internal class ClientNetworkProxy
    {
        /// <summary>
        /// 创建客户端网络代理实例。
        /// </summary>
        public ClientNetworkProxy() { }

        /// <summary>
        /// 处理服务器发来的握手确认消息，记录玩家 ID 并发起首次心跳。
        /// </summary>
        /// <param name="msg">握手消息，包含分配的 PlayerId。</param>
        [MessageHandler]
        public void OnHello(HelloClientMsg msg)
        {
            if (ClientCore.PlayerId != 0) return;
            ClientCore.SetPlayerId(msg.Id);
            try { LogCore.Debug(nameof(ClientCore), $"握手成功，PlayerId: {ClientCore.PlayerId}"); } catch { }
            ClientCore.Send(new PingMsg() { ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
        }

        /// <summary>
        /// 处理服务器发来的踢出消息，关闭客户端连接。
        /// </summary>
        /// <param name="msg">断线消息。</param>
        [MessageHandler]
        public void OnBye(ByebyeClientMsg msg)
        {
            try { LogCore.Debug(nameof(ClientCore), "服务器踢出"); } catch { }
            ClientCore.Shutdown();
        }

        /// <summary>
        /// 处理服务器发来的心跳响应消息，计算 RTT 并安排下一次心跳。
        /// </summary>
        /// <param name="msg">心跳响应消息，包含客户端时间戳和服务端时间戳。</param>
        [MessageHandler]
        public void OnPong(PongMsg msg)
        {
            ClientCore.SetRTT(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - msg.ClientTimestamp);
            ClientCore._pingCts?.Cancel();
            ClientCore._pingCts = new CancellationTokenSource();
            SchedulePing(msg.ServerTimestamp).Forget();
        }

        private async UniTaskVoid SchedulePing(long serverTimestamp)
        {
            var cts = ClientCore._pingCts;
            if (cts == null) return;
            await UniTask.Delay(5000, cancellationToken: cts.Token);
            if (!ClientCore.IsConnected) return;
            ClientCore.Send(new PingMsg() { ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ServerTimestamp = serverTimestamp });
        }
    }
}
