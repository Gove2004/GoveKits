using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ServerCore
    {
        public static bool IsListening { get; private set; }
        public static event Action<int> OnClientConnected;
        public static event Action<int, string> OnClientDisconnected;

        private static TcpListener _listener;
        private static readonly ConcurrentDictionary<int, Session> _sessions = new();
        private static int _nextSessionId = 1;
        
        // 框架内置消息代理
        private static readonly ServerNetworkProxy _proxy = new ServerNetworkProxy();

        public static void StartServer(int port)
        {
            Shutdown();
            
            // 绑定内置的 Proxy，接管 Hello 和 Bye 消息
            DispatcherCore.Bind(_proxy);
            
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsListening = true;
            AcceptLoop().Forget();
            LogCore.Success(nameof(ServerCore), $"TCP 监听启动: {port}");
        }

        public static async UniTask StartHost(int port)
        {
            StartServer(port);
            await ClientCore.ConnectAsync("127.0.0.1", port);
        }

        private static async UniTaskVoid AcceptLoop()
        {
            while (IsListening)
            {
                try
                {
                    var socket = await _listener.AcceptSocketAsync();
                    int id = _nextSessionId++;
                    var session = new Session(id, new TcpConnection(socket));
                    session.OnClosed += RemoveSession;

                    _sessions.TryAdd(id, session);
                    OnClientConnected?.Invoke(id);
                }
                catch (Exception ex)
                {
                    if (IsListening) LogCore.Error(nameof(ServerCore), $"监听错误: {ex.Message}");
                }
            }
        }

        public static bool TryGetSession(int sessionId, out Session session)
            => _sessions.TryGetValue(sessionId, out session);

        public static void SendTo<T>(int sessionId, T message) where T : IProtocolMessage
        {
            if (TryGetSession(sessionId, out var session)) session.Send(message);
        }

        public static void Broadcast<T>(T message, int excludeSessionId = -1) where T : IProtocolMessage
        {
            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return;

            byte[] purePayload = ProtocolCore.Serialize(message);
            byte[] frameData = new byte[2 + purePayload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(frameData, id);
            Buffer.BlockCopy(purePayload, 0, frameData, 2, purePayload.Length);

            foreach (var kvp in _sessions)
            {
                if (kvp.Key != excludeSessionId) kvp.Value.SendRaw(frameData);
            }
        }

        private static void RemoveSession(Session session, string reason)
        {
            if (_sessions.TryRemove(session.SessionId, out _))
            {
                OnClientDisconnected?.Invoke(session.SessionId, reason);
            }
        }

        public static void Shutdown()
        {
            IsListening = false;
            _listener?.Stop();
            foreach (var session in _sessions.Values) session.Kick("Server Shutdown");
            _sessions.Clear();
            
            // 解绑内置 Proxy
            DispatcherCore.Unbind(_proxy);
        }

        // =========================================================
        // 内置消息代理：处理底层握手与心跳
        // =========================================================
        private class ServerNetworkProxy
        {
            [MessageHandler]
            public void OnHello(Session session, HelloServerMsg msg)
            {
                // 收到客户端打招呼后，给它分配身份并回信
                LogCore.Info(nameof(ServerCore), $"收到客户端打招呼 SessionId: {session.SessionId}");
                session.Send(new HelloClientMsg { Id = session.SessionId, Msg = "Welcome to GoveKits Server!" });
            }

            [MessageHandler]
            public void OnBye(Session session, ByebyeServerMsg msg)
            {
                // 客户端主动要求断开
                session.Kick("Client requested bye");
            }

            [MessageHandler]
            public void OnPing(Session session, PingMsg msg)
            {
                session.RTT = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - msg.ServerTimestamp;
                // LogCore.Debug(nameof(ServerNetworkProxy), $"收到 Ping，SessionId: {session.SessionId}, RTT = {session.RTT} ms");

                // 回复心跳，可用于测算延迟 (RTT)
                session.Send(new PongMsg { 
                    ClientTimestamp = msg.ClientTimestamp,
                    ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() 
                });
            }
        }
    }
}