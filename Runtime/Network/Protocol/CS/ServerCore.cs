using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 服务端核心，管理 TCP 监听、会话和帧同步服务端组件。
    /// </summary>
    public static class ServerCore
    {
        private static ProtocolCenter _protocol;
        private static MessageDispatcher _dispatcher;
        private static ServerNetworkProxy _proxy;
        private static readonly ServerCollector _serverCollector = new();
        private static ServerSimulator _serverSimulator;

        private static TcpListener _listener;
        private static bool _isListening;
        private static readonly ConcurrentDictionary<int, Session> _sessions = new();
        private static int _nextSessionId = 1;

        /// <summary>
        /// 协议中心实例，用于注册和序列化协议消息。
        /// </summary>
        public static ProtocolCenter Protocol => _protocol;

        /// <summary>
        /// 消息分发器实例，负责将协议 ID 路由到对应的处理器。
        /// </summary>
        public static MessageDispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// 服务端是否正在监听连接。
        /// </summary>
        public static bool IsListening => _isListening;

        /// <summary>
        /// 有新客户端连接成功时触发，参数为客户端会话 ID。
        /// </summary>
        public static event Action<int> OnClientConnected;

        /// <summary>
        /// 客户端断开连接时触发，参数为会话 ID 和断开原因。
        /// </summary>
        public static event Action<int, string> OnClientDisconnected;

        /// <summary>
        /// 服务端逻辑 Tick 触发时触发，参数为所有玩家输入包和 Tick 间隔时间。
        /// </summary>
        public static event Action<AllInputPackage, float> OnServerLogicTick;

        /// <summary>
        /// 初始化服务端核心。
        /// </summary>
        public static void Setup()
        {
            _protocol = new ProtocolCenter();
            _dispatcher = new MessageDispatcher(_protocol);
            _proxy = new ServerNetworkProxy();
        }

        /// <summary>
        /// 启动服务器监听。
        /// </summary>
        public static void StartServer(int port)
        {
            Shutdown();

            _dispatcher.Bind(_proxy);

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isListening = true;
            AcceptLoop().Forget();

            try { LogCore.Success(nameof(ServerCore), $"服务器已启动，监听端口 {port}"); } catch { }
        }

        private static async UniTaskVoid AcceptLoop()
        {
            while (_isListening)
            {
                try
                {
                    var socket = await _listener.AcceptSocketAsync();

                    int id = Interlocked.Increment(ref _nextSessionId);
                    var connection = new TcpChannel(socket);
                    var session = new Session(id, connection, _dispatcher, _protocol);

                    session.OnClosed += RemoveSession;
                    _sessions.TryAdd(id, session);

                    OnClientConnected?.Invoke(id);
                }
                catch (Exception ex)
                {
                    if (_isListening) try { LogCore.Error(nameof(ServerCore), $"监听错误: {ex.Message}"); } catch { }
                }
            }
        }

        /// <summary>
        /// 尝试根据会话 ID 获取对应的 Session 对象。
        /// </summary>
        /// <param name="sessionId">会话 ID。</param>
        /// <param name="session">找到的 Session 对象，未找到时为 null。</param>
        /// <returns>找到会话时返回 true。</returns>
        public static bool TryGetSession(int sessionId, out Session session)
            => _sessions.TryGetValue(sessionId, out session);

        /// <summary>
        /// 向指定会话发送协议消息。
        /// </summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="sessionId">目标会话 ID。</param>
        /// <param name="message">要发送的消息实例。</param>
        public static void SendTo<T>(int sessionId, T message) where T : IProtocolMessage
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Send(message);
            }
        }

        /// <summary>
        /// 向所有连接的客户端广播协议消息（可选排除某个会话）。
        /// </summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="message">要广播的消息实例。</param>
        /// <param name="excludeSessionId">要排除的会话 ID，默认为 -1（不排除任何会话）。</param>
        public static void Broadcast<T>(T message, int excludeSessionId = -1) where T : IProtocolMessage
        {
            ushort id = _protocol.GetId<T>();
            if (id == 0) return;

            byte[] purePayload = _protocol.Serialize(message);
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

        /// <summary>
        /// 启动服务端帧同步。
        /// </summary>
        public static void StartServerSync(float tickInterval = 0.05f)
        {
            _serverSimulator = new ServerSimulator(_serverCollector, tickInterval);
            _serverSimulator.OnLogicTick += (data) => OnServerLogicTick?.Invoke(data, tickInterval);
            _serverSimulator.OnBroadcastTick += BroadcastStates;
        }

        /// <summary>
        /// 驱动服务端帧同步组件。
        /// </summary>
        public static void Update(float deltaTime)
        {
            _serverSimulator?.Update(deltaTime);
        }

        private static void BroadcastStates(int currentTick)
        {
            var changedList = new List<EntityState>();
            var allEntities = SpawnCore.GetAllEntities();

            foreach (var entity in allEntities)
            {
                if (entity is ISyncable syncable && syncable.IsDirty)
                {
                    changedList.Add(new EntityState
                    {
                        NetId = syncable.NetId,
                        StatePayload = syncable.GetState().Payload
                    });
                    syncable.IsDirty = false;
                }
            }

            if (changedList.Count > 0)
            {
                Broadcast(new WorldPackage
                {
                    Tick = currentTick,
                    Entities = changedList.ToArray()
                });
            }
        }

        /// <summary>
        /// 关闭服务器，释放资源。
        /// </summary>
        public static void Shutdown()
        {
            _isListening = false;
            _listener?.Stop();

            foreach (var session in _sessions.Values) session.Kick("服务端关闭");
            _sessions.Clear();

            _dispatcher.Unbind(_proxy);
        }

        public static void Close() => Shutdown();

        /// <summary>
        /// 内部代理，处理握手和心跳。
        /// </summary>
        private class ServerNetworkProxy
        {
            [MessageHandler]
            public void OnHello(Session session, HelloServerMsg msg)
            {
                try { LogCore.Debug(nameof(ServerCore), $"收到打招呼 SessionId: {session.SessionId}"); } catch { }
                session.Send(new HelloClientMsg { Id = session.SessionId, Msg = "欢迎进入 GoveKits 服务器！" });
            }

            [MessageHandler]
            public void OnBye(Session session, ByebyeServerMsg msg)
            {
                session.Kick("客户端请求断开");
            }

            [MessageHandler]
            public void OnPing(Session session, PingMsg msg)
            {
                session.RTT = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - msg.ServerTimestamp;

                session.Send(new PongMsg {
                    ClientTimestamp = msg.ClientTimestamp,
                    ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            }
        }
    }
}
