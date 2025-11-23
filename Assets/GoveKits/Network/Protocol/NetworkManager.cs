using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetManager : MonoSingleton<NetManager>
    {
        // --- 配置 ---
        public string RemoteIP = "127.0.0.1";
        public int RemotePort = 12345;

        // --- 核心组件 ---
        private NetworkClient Client { get; set; }

        // --- 游戏状态 (这些是 NetworkClient 不关心的，但 Manager 必须关心的) ---
        public int PlayerID { get; private set; } = 0;
        public bool IsLogged { get; private set; } = false;
        
        // 封装连接状态：只有 Socket 连上且拿到 PlayerID 才算真正连入游戏
        public bool IsConnected => Client != null && Client.IsConnected && IsLogged;

        // --- 全局事件 (供 UI 订阅) ---
        public event Action<int> OnLoginSuccess;
        public event Action OnDisconnected;

        private void Awake()
        {
            // 初始化底层客户端
            Client = new NetworkClient();
            
            // ★ 关键：Manager 自己也是一个监听者，它要监听系统级消息
            Client.Bind(this);
            
            // 自动连接
            ConnectToServer().Forget();
        }

        private async UniTaskVoid ConnectToServer()
        {
            // 1. 建立 TCP 连接
            await Client.ConnectAsync(RemoteIP, RemotePort);
            
            if (Client.IsConnected)
            {
                // 2. 连接成功，发送登录请求 (这里可以扩展成真正的认证流程)
            }
            else
            {
                // 3. 连接失败，通知 UI
            }
        }

        // --- 核心职责：处理登录 ---
        // 这是 NetManager 存在的最大意义：拦截系统消息，建立玩家身份
        [MessageHandler(Protocol.PlayerInitID)] 
        private void HandleLogin(InitMsg msg)
        {
            this.PlayerID = msg.Body.PlayerID;
            this.IsLogged = true;
            
            Debug.Log($"[NetManager] PlayerID Init = {PlayerID}");
            
            // 通知 UI 或 场景加载器
            OnLoginSuccess?.Invoke(PlayerID);
        }

        // --- 核心职责：发送封装 ---
        // 提供便捷入口，同时可以做发送前的检查（比如是否已登录）
        public void Send(Message msg)
        {
            if (Client != null && Client.IsConnected)
            {
                Client.Send(msg);
            }
            else
            {
                Debug.LogWarning("[NetManager] Cannot send message: Not Connected.");
            }
        }
        
        // --- 代理方法 (Optional) ---
        public void Bind(object target) => Client?.Bind(target);
        public void Unbind(object target) => Client?.Unbind(target);

        // --- 生命周期管理 ---
        public void Close() => Client?.Dispose();
        protected override void OnDestroy()
        {
            if (Client != null)
            {
                Client.Unbind(this); // 解绑自己
                Client.Dispose();    // 断开连接
            }
            OnDisconnected?.Invoke();
            base.OnDestroy();
        }
    }




    [Message(Protocol.PlayerInitID)]
    public class InitMsg : Message<InitBody> { }

    public class InitBody : MessageBody 
    {
        public int PlayerID;
        public override int Length() => 4;
        public override void Writing(byte[] b, ref int i) => WriteInt(b, PlayerID, ref i);
        public override void Reading(byte[] b, ref int i) => PlayerID = ReadInt(b, ref i);
    }
}