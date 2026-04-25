using System;

namespace GoveKits.Runtime.Network
{
    public static class StateClientCore
    {
        private static StateLerper _lerper;
        private static IInputSubmitor _inputor;
        private static readonly StateClientProxy _proxy = new StateClientProxy();

        // 客户端事件：渲染表现层的插值驱动
        public static event Action<WorldPackage, WorldPackage, float> OnInterpolationTick;

        public static void Start(IInputSubmitor inputor, float interpolationDelay = 0.1f)
        {
            _inputor = inputor;
            _inputor.OnSubmit += (playerId, protocolID, payload) => SendStateInput(playerId, payload);
            _lerper = new StateLerper { InterpolationDelay = interpolationDelay };
            _lerper.OnInterpolationTick += (from, to, t) => OnInterpolationTick?.Invoke(from, to, t);
            
            ClientCore.Dispatcher.Bind(_proxy);
        }

        public static void Stop()
        {
            _lerper = null;
            _inputor = null;
            ClientCore.Dispatcher.Unbind(_proxy);
        }

        public static void Update(float deltaTime)
        {
            _inputor?.Update(deltaTime);
            _lerper?.Update(deltaTime);
        }

        public static void SubmitModify<T>(Action<T> modifier) where T : IProtocolMessage, new()
        {
            if (_inputor is InputSubmitor<T> typedInputor)
            {
                typedInputor.ModifyInput(modifier);
            }
        }

        internal static void SendStateInput(int playerId, byte[] payload)
        {
            ClientCore.Send(new PlayerInputPackage { PlayerId = playerId, Payload = payload });
        }

        private class StateClientProxy
        {
            [MessageHandler]
            public void OnReceiveSnapshot(WorldPackage snapshot) => _lerper?.EnqueueSnapshot(snapshot);
        }
    }
}