using GoveKits.Singleton; // 假设你有 MonoSingleton
using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        public MessageDispatcher Dispatcher { get; private set; }
        public NetworkClient Client { get; private set; }

        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 1. 注册协议
            MessageBuilder.AutoRegisterAll();
            
            // 2. 初始化核心
            Dispatcher = new MessageDispatcher();
            Client = new NetworkClient(Dispatcher);
            
            // 3. 绑定自身消息处理
            Dispatcher.Bind(this);
        }

        public void Connect(string ip, int port) => Client.Connect(ip, port);
        public void Disconnect() => Client.Disconnect();
        public void Send(Message msg) => Client.Send(msg);

        // 生命周期管理
        public override void OnDestroy()
        {
            base.OnDestroy();
            Client?.Disconnect();
        }
        private void OnApplicationQuit() => Client?.Disconnect();
    }
}