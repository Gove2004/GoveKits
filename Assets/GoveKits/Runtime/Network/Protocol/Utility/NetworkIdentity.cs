using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkIdentity : MonoBehaviour
    {
        public string PrefabName;
        public int NetID = 0;
        public int OwnerID = 0; // 0代表服务器/场景物体
        
        // 缓存该物体上所有的 NetworkBehaviour
        private NetworkBehaviour[] _behaviours;

        public bool IsMine
        {
            get
            {
                if (NetworkManager.Instance == null) return false;
                // Host 拥有 Server 权限和 OwnerID=1 的权限
                if (NetworkManager.Instance.IsHost)
                    return OwnerID == 0 || OwnerID == NetworkManager.HostPlayerID;
                
                return NetworkManager.Instance.MyPlayerID == OwnerID;
            }
        }

        private void Awake()
        {
            _behaviours = GetComponents<NetworkBehaviour>();
        }

        private void Start()
        {
            // 注册自己 (如果是动态生成的物体，需要在生成时分配 NetID)
            if (NetID > 0)
            {
                NetworkManager.Instance.RegisterObject(this);
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.UnregisterObject(this);
            }
        }

        // 收到 RPC 消息后，分发给所有 Behaviour
        public void InvokeRPCLocal(string methodName, object[] parameters)
        {
            foreach (var behaviour in _behaviours)
            {
                // 尝试在每个 Behaviour 上调用，如果成功了一个就停止吗？
                // 通常建议 RPC 方法名在同一个物体上唯一
                if (behaviour.InvokeRPC(methodName, parameters))
                {
                    return; // 找到并执行后返回
                }
            }
            Debug.LogWarning($"[RPC] Method '{methodName}' not found on NetID {NetID}");
        }
    }
}