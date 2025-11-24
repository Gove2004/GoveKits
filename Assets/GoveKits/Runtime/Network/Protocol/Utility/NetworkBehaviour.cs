using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable() => NetworkManager.Instance?.Bind(this);
        protected virtual void OnDisable() => NetworkManager.Instance?.Unbind(this);




        protected virtual void Awake()
        {
            // 预先缓存所有带 [Rpc] 标签的方法
            _rpcCache = new Dictionary<string, MethodInfo>();
            var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<RpcAttribute>() != null)
                {
                    _rpcCache[method.Name] = method;
                }
            }
        }






        // 发送普通网络消息
        protected void SendMessage(Message msg)
        {
            NetworkManager.Instance.Send(msg);
        }






        private NetworkIdentity _identity;
        public NetworkIdentity Identity => _identity ? _identity : (_identity = GetComponent<NetworkIdentity>());

        public int NetID => Identity.NetID;
        public bool IsMine => Identity.IsMine;


        // 辅助发送同步消息
        protected void SendSync(SyncMessage msg)
        {
            msg.NetID = this.NetID;
            NetworkManager.Instance.Send(msg);
        }






        // --- RPC 缓存 (优化反射性能) ---
        private Dictionary<string, MethodInfo> _rpcCache = new Dictionary<string, MethodInfo>();


        // 辅助发送RPC调用
        protected void CallRPC(string methodName, params object[] parameters)
        {
            if (NetID == 0) 
            {
                Debug.LogError("Cannot send RPC on object without NetID");
                return;
            }

            // 1. 构造消息
            var msg = new RPCMessage(this.NetID, methodName, parameters);

            // 2. 发送网络消息
            NetworkManager.Instance.Send(msg);

            // 3. (可选) 如果是 Host 模式或预测逻辑，可能需要立即执行本地
            // InvokeRpcLocal(methodName, args); 
        }

        // 辅助唤起本地RPC, 返回是否成功调用
        public bool InvokeRPC(string methodName, object[] parameters)
        {
            try
            {
                if (_rpcCache.TryGetValue(methodName, out MethodInfo method))
                {
                    method.Invoke(this, parameters);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RPC] Error invoking RPC '{methodName}' on '{this.GetType().Name}': {ex}");
            }
            return false;
        }
    }
}