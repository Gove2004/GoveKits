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
        }

        protected virtual void OnEnable() => NetworkManager.Instance?.Bind(this);
        protected virtual void OnDisable() => NetworkManager.Instance?.Unbind(this);

        // --- 核心功能 ---

        protected void SendSync(SyncMessage msg)
        {
            if (!IsMine) return; // 只有拥有者才能发送同步
            msg.NetID = this.NetID;
            NetworkManager.Instance.SendToServer(msg);
        }

        /// <summary>
        /// 调用 RPC 方法
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <param name="target">目标 (Server, All, Others)</param>
        /// <param name="parameters">参数</param>
        protected void CallRPC(string methodName, RpcTarget target, params object[] parameters)
        {
            if (NetID == 0) return;

            // 1. 如果我是 Server/Host 且目标包含 Server，直接执行本地
            if (NetworkManager.Instance.IsServer)
            {
                // 服务器直接执行
                InvokeRPC(methodName, parameters);
                
                // 如果是 All 或 Others，广播给客户端
                if (target == RpcTarget.All || target == RpcTarget.Others)
                {
                    var msg = new RPCMessage(NetID, methodName, parameters);
                    NetworkManager.Instance.Broadcast(msg);
                }
            }
            // 2. 如果我是 Client
            else
            {
                // 构造消息发给 Server
                var msg = new RPCMessage(NetID, methodName, parameters);
                NetworkManager.Instance.SendToServer(msg);
                
                // 如果是 All (包含自己)，且不是 Host (Host已经在上面Server逻辑执行了)，预测执行
                // 但通常 RPC 都是由 Server 确认后再下发的，这里建议先不执行本地，等待 Server 回传
            }
        }

        // 简化版重载，默认发给所有人
        protected void CallRPC(string methodName, params object[] parameters) 
            => CallRPC(methodName, RpcTarget.All, parameters);

        // 被 NetworkIdentity 调用
        public bool InvokeRPC(string methodName, object[] parameters)
        {
            if (_rpcCache.TryGetValue(methodName, out MethodInfo method))
            {
                try
                {
                    // 参数类型转换处理 (Json反序列化时参数可能变成了 JObject 或 long/int 不匹配)
                    // 这里假设 Parser 已经处理好了类型
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
    }
}