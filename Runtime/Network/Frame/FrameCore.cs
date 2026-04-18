using System;


namespace GoveKits.Runtime.Network
{
    public static class FrameCore
    {
        private static ClientFrameExecutor _clientExecutor;
        private static ServerFrameCollector _serverCollector;
        private static ServerFrameStoreger _serverStoreger;
        private static IClientSubmitor _clientSubmitor;

        private static ClientNetworkProxy _clientProxy = new();
        private static ServerNetworkProxy _serverProxy = new();

        public static event Action<FramePackage> OnFrameTick;
        public static bool IsCatchingUp => _clientExecutor?.IsCatchingUp ?? false;

        public static void StartClient(IClientSubmitor submitor, float tickInterval = 0.033f)
        {
            _clientSubmitor = submitor;
            _clientExecutor = new ClientFrameExecutor { TickInterval = tickInterval };
            _clientExecutor.OnFrameExecuted += (frame) => OnFrameTick?.Invoke(frame);

            DispatcherCore.Bind(_clientProxy);

            // 客户端启动后，主动向服务器发送握手追帧请求
            // 如果本地保存了录像，可以传真实的 LocalFrameId。这里默认新加入传 0。
            ClientCore.Send(new SyncFrameRequestMsg { PlayerId = submitor.PlayerId, LocalFrameId = 0 });
        }

        public static void StartServer(float tickInterval = 0.033f)
        {
            _serverStoreger = new ServerFrameStoreger();
            _serverCollector = new ServerFrameCollector(_serverStoreger) { TickInterval = tickInterval };
            
            DispatcherCore.Bind(_serverProxy);
        }

        public static void StartHost(IClientSubmitor submitor, float tickInterval = 0.033f)
        {
            StartServer(tickInterval);
            StartClient(submitor, tickInterval);
        }

        public static void Stop()
        {
            _clientExecutor = null;
            _clientSubmitor = null;
            _serverCollector = null;
            _serverStoreger?.Clear();
            _serverStoreger = null;
            DispatcherCore.Unbind(_clientProxy);
            DispatcherCore.Unbind(_serverProxy);
        }

        public static void Update(float deltaTime)
        {
            _serverCollector?.Update(deltaTime);
            
            // 优化：追帧期间，客户端不应该提交新的操作
            if (!IsCatchingUp) 
            {
                _clientSubmitor?.Update(deltaTime);
            }
            
            _clientExecutor?.Update(deltaTime);
        }

        public static void ServerInject<T>(T commandData) where T : IProtocolMessage
        {
            if (_serverCollector == null) return;

            var inputPkg = new FrameInputPackage
            {
                PlayerId = 0, // 0 代表系统/上帝
                Payload = ProtocolCore.Serialize(commandData)
            };

            // 直接塞进收集器里，下一帧会和玩家指令一起广播出去！
            _serverCollector.CollectInput(inputPkg);
        }

        public static void SubmitModify<T>(Action<T> modifier) where T : IProtocolMessage, new()
        {
            if (_clientSubmitor is ClientFrameSubmitor<T> typedSubmitor && !IsCatchingUp)
            {
                typedSubmitor.ModifyInput(modifier);
            }
        }

        internal static void SendInput(int playerId, byte[] payload)
        {
            ClientCore.Send(new FrameInputPackage { PlayerId = playerId, Payload = payload });
        }

        // --- 网络代理 ---
        private class ClientNetworkProxy
        {
            [MessageHandler]
            public void OnReceiveFramePackage(FramePackage frame) => _clientExecutor?.EnqueueFrame(frame);

            [MessageHandler]
            public void OnReceiveSyncResponse(SyncFrameResponseMsg msg)
            {
                _clientExecutor?.StartCatchUpPhase();

                if (msg.HistoryFrames != null)
                {
                    foreach (var frame in msg.HistoryFrames)
                        _clientExecutor?.EnqueueFrame(frame);
                }

                if (msg.IsEnd)
                {
                    _clientExecutor?.EndCatchUpPhase();
                }
            }
        }

        private class ServerNetworkProxy
        {
            [MessageHandler]
            public void OnReceiveInput(int channelId, FrameInputPackage input) => _serverCollector?.CollectInput(input);

            [MessageHandler]
            public void OnReceiveSyncRequest(int channelId, SyncFrameRequestMsg req)
            {
                // 服务端收到客户端的追帧请求，开始提取档案下发
                _serverStoreger?.SendHistoryToClient(channelId, req.LocalFrameId);
            }
        }
    }
}