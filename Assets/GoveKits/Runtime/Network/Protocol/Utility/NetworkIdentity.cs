using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkIdentity : MonoBehaviour
    {
        public string PrefabName;
        public int NetID = 0;
        public int OwnerID = 0; // 0代表服务器/场景物体
        
        private NetworkBehaviour[] _behaviours;

        public bool IsMine
        {
            get
            {
                if (NetworkManager.Instance == null) return false;
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
            // 【修改】向 SpawnerManager 注册
            if (NetID > 0 && SpawnerManager.Instance != null)
            {
                SpawnerManager.Instance.RegisterObject(this);
            }
        }

        private void OnDestroy()
        {
            // 【修改】向 SpawnerManager 注销
            if (SpawnerManager.Instance != null)
            {
                SpawnerManager.Instance.UnregisterObject(this);
            }
        }

        public void InvokeRPCLocal(string methodName, object[] parameters)
        {
            foreach (var behaviour in _behaviours)
            {
                if (behaviour.InvokeRPC(methodName, parameters))
                {
                    return; 
                }
            }
            Debug.LogWarning($"[RPC] Method '{methodName}' not found on NetID {NetID}");
        }
    }
}