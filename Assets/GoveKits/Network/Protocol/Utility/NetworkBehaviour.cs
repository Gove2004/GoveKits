using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkBehaviour : MonoBehaviour
    {
        // 注册与解绑
        protected virtual void OnEnable() => NetManager.Instance?.Bind(this);

        protected virtual void OnDisable() => NetManager.Instance?.Unbind(this);



        // ========== NetworkIdentity 用于同步属性 ==========
        

        private NetworkIdentity _identity;
        public NetworkIdentity Identity => _identity ? _identity : (_identity = GetComponent<NetworkIdentity>());

        public int NetID => Identity.NetID;
        public bool IsMine => Identity.IsMine;

        /// <summary>
        /// 发送同步消息的辅助方法
        /// </summary>
        /// <typeparam name="TBody">Body类型</typeparam>
        /// <param name="msg">完整的消息对象</param>
        protected void SendSync<TBody>(Message<SyncBody<TBody>> msg) where TBody : MessageBody, new()
        {
            // 自动填充 NetID，防止写漏
            msg.Body.NetID = this.NetID;
            Debug.Log($"[NetworkBehaviour] Sending Sync MsgID: {msg.MsgID} for NetID: {msg.Body.NetID}");
            NetManager.Instance.Send(msg);
        }
    }
}