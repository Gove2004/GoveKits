using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GoveKits.Network
{
    public class SpawnerManager : MonoSingleton<SpawnerManager>
    {
        private const string PREFAB_PATH = "NetPrefabs/";

        // 这里是唯一的 NetworkIdentity 注册表
        private readonly Dictionary<int, NetworkIdentity> _activeObjects = new();
        private readonly Dictionary<string, NetworkIdentity> _prefabCache = new();

        private void Start()
        {
            NetworkManager.Instance.Bind(this);
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnClientConnected += OnPlayerJoined;
                // 如果断开连接，清空所有物体
                NetworkManager.Instance.OnServerDisconnected += CleanupAllObjects;
            }
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Unbind(this);
                NetworkManager.Instance.OnClientConnected -= OnPlayerJoined;
                NetworkManager.Instance.OnServerDisconnected -= CleanupAllObjects;
            }
            CleanupAllObjects();
            base.OnDestroy();
        }

        // =========================================================
        // 公共管理接口 (核心变动)
        // =========================================================

        public void RegisterObject(NetworkIdentity identity)
        {
            if (!_activeObjects.ContainsKey(identity.NetID))
            {
                _activeObjects.Add(identity.NetID, identity);
            }
            else
            {
                Debug.LogWarning($"[Spawner] NetID {identity.NetID} already registered!");
            }
        }

        public void UnregisterObject(NetworkIdentity identity)
        {
            if (_activeObjects.ContainsKey(identity.NetID))
            {
                _activeObjects.Remove(identity.NetID);
            }
        }
        
        // 为了兼容旧代码习惯，提供 UnregisterObject(int) 重载
        public void UnregisterObject(int netId)
        {
            if (_activeObjects.ContainsKey(netId))
            {
                _activeObjects.Remove(netId);
            }
        }

        public NetworkIdentity GetObject(int netId)
        {
            _activeObjects.TryGetValue(netId, out var identity);
            return identity;
        }
        
        private void CleanupAllObjects()
        {
            // 清理场景中所有网络物体
            var list = _activeObjects.Values.ToList();
            _activeObjects.Clear();
            foreach (var obj in list)
            {
                if(obj != null) Destroy(obj.gameObject);
            }
        }

        // =========================================================
        // 状态同步 (Late Join)
        // =========================================================
        private void OnPlayerJoined(int newPlayerID)
        {
            if (!NetworkManager.Instance.IsHost) return;
            if (newPlayerID == NetworkManager.Instance.MyPlayerID) return;
            if (_activeObjects.Count == 0) return;

            Debug.Log($"[Spawner] Syncing {_activeObjects.Count} objects to Player {newPlayerID}...");

            foreach (var kvp in _activeObjects.ToList()) 
            {
                var identity = kvp.Value;
                if (identity == null) continue;

                var msg = new SpawnMessage
                {
                    NetID = identity.NetID,
                    OwnerID = identity.OwnerID,
                    PrefabName = identity.PrefabName, 
                    Pos = identity.transform.position, 
                    Rot = identity.transform.eulerAngles
                };

                NetworkManager.Instance.SendToPlayer(newPlayerID, msg);
            }
        }

        // =========================================================
        // 处理生成
        // =========================================================

        [MessageHandler(Protocol.SpawnID)]
        private void OnHandleSpawn(SpawnMessage msg)
        {
            if (_activeObjects.ContainsKey(msg.NetID)) return;

            NetworkIdentity prefab = GetCachedPrefab(msg.PrefabName);
            if (prefab == null) return;

            NetworkIdentity instance = Instantiate(prefab, msg.Pos, Quaternion.Euler(msg.Rot));
            
            instance.NetID = msg.NetID;
            instance.OwnerID = msg.OwnerID;
            instance.PrefabName = msg.PrefabName; 
            instance.name = $"{msg.PrefabName}_{msg.NetID}";

            // 注意：instance.Start() 会调用 RegisterObject，但 Instantiate 实际上会立即调用 Awake/Start 吗？
            // 在 Unity 中，Instantiate 后 Awake 立即执行，Start 在下一帧。
            // 建议手动注册或者依赖 NetworkIdentity.Start
            // 这里为了保险，如果不依赖 NetworkIdentity.Start，可以手动注册
            // 但因为 NetworkIdentity 也在 Start 里注册，需要避免重复。
            // 最佳实践：NetworkIdentity.NetID 赋值后，NetworkIdentity.Start 负责注册。
            // 这里仅仅初始化数据。
        }

        // =========================================================
        // 处理销毁
        // =========================================================

        [MessageHandler(Protocol.DespawnID)]
        private void OnHandleDespawn(DespawnMessage msg)
        {
            if (_activeObjects.TryGetValue(msg.NetID, out var identity))
            {
                UnregisterObject(msg.NetID); 
                if (identity != null) Destroy(identity.gameObject);
            }
        }

        // =========================================================
        // 辅助与公共接口
        // =========================================================

        public void SpawnObject(string prefabName, int ownerId, Vector3 pos, Vector3 rot)
        {
            if (!NetworkManager.Instance.IsConnected) return;
            
            if (NetworkManager.Instance.IsHost)
            {
                int newNetId = Random.Range(1000, 999999); 
                while(_activeObjects.ContainsKey(newNetId)) newNetId = Random.Range(1000, 999999);

                var msg = new SpawnMessage
                {
                    NetID = newNetId,
                    OwnerID = ownerId,
                    PrefabName = prefabName,
                    Pos = pos,
                    Rot = rot
                };

                NetworkManager.Instance.Broadcast(msg); 
            }
        }

        private NetworkIdentity GetCachedPrefab(string name)
        {
            if (_prefabCache.TryGetValue(name, out var cachedPrefab)) return cachedPrefab;
            
            string fullPath = PREFAB_PATH + name;
            var resource = Resources.Load<NetworkIdentity>(fullPath);
            if (resource != null) _prefabCache[name] = resource;
            
            return resource;
        }
    }
}