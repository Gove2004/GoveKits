using GoveKits.Singleton; 


namespace GoveKits.Network
{
    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        public MessageDispatcher Dispatcher { get; private set; }
        public NetworkClient Client { get; private set; }

        protected void Awake()
        {
            // 1. 初始化核心
            Dispatcher = new MessageDispatcher();
            Client = new NetworkClient(Dispatcher);
            
            // 2. 绑定自身
            Dispatcher.Bind(this);
            
            // 3. 注册协议 (这部分通常由工具生成或手动写)
            MessageRegistry.ScanAndRegister<BuiltinMsgId>();
        }

        public void Connect(string ip, int port) => Client.Connect(ip, port);
        public void Disconnect() => Client.Disconnect();
        public void Send(Google.Protobuf.IMessage msg) => Client.Send(msg);

        public override void OnDestroy() => Client?.Disconnect();
        private void OnApplicationQuit() => Client?.Disconnect();
    }
}