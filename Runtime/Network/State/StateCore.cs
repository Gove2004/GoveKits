using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Network
{
    public static class StateCore
    {
        private static ClientStateLerper _clientLerper;
        private static IClientStateInputor _clientInputor;
        private static ServerStateSimulator _serverSimulator;

        // --- 客户端事件：渲染表现层的插值驱动 ---
        public static event Action<WorldPackage, WorldPackage, float> OnInterpolationTick;

        // --- 服务端事件：服务端权威逻辑推演 ---
        public static event Action<List<StateInputPackage>> OnServerLogicTick;

        // --- 初始化 ---
        public static void StartClient(IClientStateInputor inputor, float interpolationDelay = 0.1f)
        {
            _clientInputor = inputor;
            _clientLerper = new ClientStateLerper { InterpolationDelay = interpolationDelay };
            _clientLerper.OnInterpolationTick += (from, to, t) => OnInterpolationTick?.Invoke(from, to, t);
            
            DispatcherCore.Bind(new ClientStateProxy());
        }

        public static void StartServer(float serverTickInterval = 0.05f)
        {
            _serverSimulator = new ServerStateSimulator { TickInterval = serverTickInterval };
            _serverSimulator.OnServerLogicTick += (inputs) => OnServerLogicTick?.Invoke(inputs);
            
            DispatcherCore.Bind(new ServerStateProxy());
        }

        public static void StartHost(IClientStateInputor inputor, float serverTickInterval = 0.05f)
        {
            StartServer(serverTickInterval);
            StartClient(inputor);
        }

        public static void Stop()
        {
            _clientLerper = null;
            _clientInputor = null;
            _serverSimulator = null;
            DispatcherCore.Unbind(new ClientStateProxy());
            DispatcherCore.Unbind(new ServerStateProxy());
        }

        public static void Update(float deltaTime)
        {
            _serverSimulator?.Update(deltaTime);
            _clientInputor?.Update(deltaTime);
            _clientLerper?.Update(deltaTime);
        }

        // --- 客户端 API：极其优雅的分布式修改 ---
        public static void SubmitModify<T>(Action<T> modifier) where T : IProtocolMessage, new()
        {
            if (_clientInputor is ClientStateInputor<T> typedInputor)
            {
                typedInputor.ModifyInput(modifier);
            }
        }

        internal static void SendStateInput(int playerId, byte[] payload)
        {
            ClientCore.Send(new StateInputPackage { PlayerId = playerId, Payload = payload });
        }

        // --- 服务端 API：写入绝对权威状态 ---
        public static void ServerSetEntityState<T>(int netId, float px, float py, float pz, float rx, float ry, float rz, T customState) where T : IProtocolMessage
        {
            if (_serverSimulator == null) return;
            byte[] payload = customState == null ? null : ProtocolCore.Serialize(customState);
            _serverSimulator.UpdateEntityState(netId, px, py, pz, rx, ry, rz, payload);
        }

        // --- 网络代理 ---
        private class ClientStateProxy
        {
            [MessageHandler]
            public void OnReceiveSnapshot(WorldPackage snapshot) => _clientLerper?.EnqueueSnapshot(snapshot);
        }

        private class ServerStateProxy
        {
            [MessageHandler]
            public void OnReceiveInput(int channelId, StateInputPackage input) => _serverSimulator?.CollectInput(input);
        }
    }
}