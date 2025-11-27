using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkBehaviour : MonoBehaviour
    {
        private NetworkIdentity _identity;
        public NetworkIdentity Identity => _identity ? _identity : (_identity = GetComponent<NetworkIdentity>());
        public int NetID => Identity.NetID;
        public bool IsMine => Identity.IsMine;

        // RPC 缓存: 方法名 -> MethodInfo
        private Dictionary<string, MethodInfo> _rpcCache = new Dictionary<string, MethodInfo>();

        protected virtual void Awake()
        {
            // 缓存所有标记了 [Rpc] 的方法
            var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<RpcAttribute>() != null)
                {
                    _rpcCache[method.Name] = method;
                }
            }

            NetworkManager.Instance.Bind(this);
        }

        protected virtual void OnEnable() => NetworkManager.Instance?.Bind(this);
        protected virtual void OnDisable() => NetworkManager.Instance?.Unbind(this);

        // --- 核心功能 ---

        protected void SendToServer(Message msg)
            => NetworkManager.Instance.SendToServer(msg);
        protected void SendToPlayer(int targetId, Message msg)
            => NetworkManager.Instance.SendToPlayer(targetId, msg);
        protected void SendToAll(Message msg, int excludeIds = -1)
            => NetworkManager.Instance.Broadcast(msg, excludeIds);

        protected void SendSync(SyncMessage msg)
        {
            if (!IsMine) return; // 只有拥有者才能发送同步
            msg.NetID = this.NetID;
            NetworkManager.Instance.SendToServer(msg);
        }

        /// <summary>
        /// 调用 RPC 方法, 约定先发送给服务器，由服务器分发给目标对象
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <param name="target">目标 (Server, All, Others)</param>
        /// <param name="parameters">参数</param>
        protected void CallRPC(string methodName, params object[] parameters)
        {
            if (NetID == 0) return;

            var msg = new RPCMessage
            {
                NetID = this.NetID,
                MethodName = methodName,
                Parameters = parameters
            };

            SendToServer(msg);
        }


        // 被 NetworkIdentity 调用
        public bool InvokeRPC(string methodName, object[] parameters)
        {
            if (_rpcCache.TryGetValue(methodName, out MethodInfo method))
            {
                try
                {
                    method.Invoke(this, parameters);
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[RPC Fail] {methodName}: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
            return false;
        }


        public virtual void OnDestroy()
        {
            NetworkManager.Instance?.Unbind(this);
        }
    }
}