using Generated;
using GoveKits.Singleton; 
using UnityEngine;

namespace GoveKits.Network
{
    // 确保最先初始化，防止 PingPong Start 时报错
    [DefaultExecutionOrder(-1000)]
    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        public MessageDispatcher Dispatcher { get; private set; }
        public NetworkClient Client { get; private set; }

        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 1. 优先注册协议
            MessageRegistry.ScanAndRegister<BuiltinMsgId>();

            // 2. 初始化核心
            Dispatcher = new MessageDispatcher();
            Client = new NetworkClient(Dispatcher);
            
            // 3. 绑定
            Dispatcher.Bind(this);
        }

        public void Connect(string ip, int port) => Client.Connect(ip, port);
        public void Disconnect() => Client.Disconnect();
        public void Send(Google.Protobuf.IMessage msg) => Client.Send(msg);

        protected override void OnDestroy() { base.OnDestroy(); Client?.Disconnect(); }
        private void OnApplicationQuit() => Client?.Disconnect();
    }
}