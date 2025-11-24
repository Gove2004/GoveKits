using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GoveKits.Network
{
    // === 生成物体消息 ===
    [Message(Protocol.SpawnID)]
    public class SpawnMessage : Message
    {
        public string PrefabName;
        public int NetID;
        public int OwnerID;
        public Vector3 Pos;
        public Vector3 Rot;
        
        protected override int BodyLength() => 4 + Encoding.UTF8.GetByteCount(PrefabName) + 8 + 2 * 3 * 4;
        protected override void BodyWriting(byte[] b, ref int i)
        {
            WriteString(b, PrefabName, ref i);
            WriteInt(b, NetID, ref i);
            WriteInt(b, OwnerID, ref i);
            WriteVector3(b, Pos, ref i);
            WriteVector3(b, Rot, ref i);
        }
        protected override void BodyReading(byte[] b, ref int i)
        {
            PrefabName = ReadString(b, ref i);
            NetID = ReadInt(b, ref i);
            OwnerID = ReadInt(b, ref i);
            Pos = ReadVector3(b, ref i);
            Rot = ReadVector3(b, ref i);
        }
    }

    // === 销毁物体消息 ===
    [Message(Protocol.DespawnID)]
    public class DespawnMessage : Message
    {
        public int NetID;
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] b, ref int i) => WriteInt(b, NetID, ref i);
        protected override void BodyReading(byte[] b, ref int i) => NetID = ReadInt(b, ref i);
    }


    public class SpawnerManager : MonoSingleton<SpawnerManager>
    {
        private const string PREFAB_PATH = "NetPrefabs/";

        // 活跃物体字典其实就是 "State Cache"
        private readonly Dictionary<int, NetworkIdentity> _activeObjects = new();
        private readonly Dictionary<string, NetworkIdentity> _prefabCache = new();

        private void Start()
        {
            NetworkManager.Instance.Bind(this);
            
            // 【新增】监听玩家加入事件，用于状态同步
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            }
        }

        protected override void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Unbind(this);
                // 【新增】注销事件
                NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            }
            base.OnDestroy();
        }

        // =========================================================
        // 【核心新增逻辑】后加入玩家的状态同步
        // =========================================================
        private void OnPlayerJoined(int newPlayerID)
        {
            // 只有服务器/Host需要负责同步状态
            if (!NetworkManager.Instance.IsServer && !NetworkManager.Instance.IsHost) return;

            // 如果是Host自己加入，或者没有活跃物体，无需处理
            if (newPlayerID == NetworkManager.Instance.MyPlayerID) return;
            if (_activeObjects.Count == 0) return;

            Debug.Log($"[Spawner] Syncing {_activeObjects.Count} objects to Player {newPlayerID}...");

            // 遍历当前所有活跃物体，给新玩家发送 Spawn 消息
            // 注意：这里使用 ToArray 或 ToList 防止在遍历时集合被修改
            foreach (var kvp in _activeObjects.ToList()) 
            {
                var identity = kvp.Value;
                if (identity == null) continue;

                var msg = new SpawnMessage
                {
                    NetID = identity.NetID,
                    OwnerID = identity.OwnerID,
                    PrefabName = identity.PrefabName, // 必须在 NetworkIdentity 中记录
                    
                    // 【关键】发送物体当前的位置，而不是它出生时的位置
                    // 这样新玩家看到的物体位置就是同步后的最新位置
                    Pos = identity.transform.position, 
                    Rot = identity.transform.eulerAngles
                };

                // 使用点对点发送，不要广播，否则老玩家会重复生成物体
                NetworkManager.Instance.SendTo(newPlayerID, msg);
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
            if (prefab == null)
            {
                Debug.LogError($"[Spawner] Prefab not found: {msg.PrefabName}");
                return;
            }

            // 实例化
            NetworkIdentity instance = Instantiate(prefab, msg.Pos, Quaternion.Euler(msg.Rot));
            
            instance.NetID = msg.NetID;
            instance.OwnerID = msg.OwnerID;
            instance.PrefabName = msg.PrefabName; // 【新增】记录PrefabName，供后续同步使用
            instance.name = $"{msg.PrefabName}_{msg.NetID}";

            Register(instance);
        }

        // =========================================================
        // 处理销毁
        // =========================================================

        [MessageHandler(Protocol.DespawnID)]
        private void OnHandleDespawn(DespawnMessage msg)
        {
            if (_activeObjects.TryGetValue(msg.NetID, out var identity))
            {
                Unregister(msg.NetID); // 先从字典移除
                if (identity != null) Destroy(identity.gameObject);
            }
        }

        // =========================================================
        // 辅助与公共接口
        // =========================================================

        public void SpawnObject(string prefabName, int ownerId, Vector3 pos, Vector3 rot)
        {
            if (!NetworkManager.Instance.IsConnected) return;
            
            // 只有 Server/Host 有权限分配 NetID 并发起生成
            // 如果 Client 想生成，需要发送 RPC 请求给 Server (此处简化为仅Server调用)
            if (NetworkManager.Instance.IsServer || NetworkManager.Instance.IsHost)
            {
                // 1. 分配 ID (简单的自增策略，实际项目可能需要回收机制)
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

                // 2. 本地执行 (Host模式)
                // 实际上 DispatchAsync 已经处理了本地回环，或者我们可以直接广播
                // 最好的做法是：服务器直接广播，客户端(包括Host的本地Client逻辑)收到消息后生成
                NetworkManager.Instance.Broadcast(msg); 
                
                // Host 特殊处理：因为 Broadcast 可能会排除自己，取决于 NetworkManager 实现
                // 如果是 Host，手动调一下本地生成以确保响应最快（可选，看 Send 实现）
                if (NetworkManager.Instance.IsHost)
                {
                    // NetworkManager.Instance.SendToSelf(msg); // 视你的架构而定
                    // 这里为了保险起见，如果 Broadcast 排除了 HostId，则需要手动触发：
                    // OnHandleSpawn(msg); 
                    // 但推荐 NetworkManager 的 Broadcast 包含 Host 自己的 LocalConnection
                }
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

        public void Register(NetworkIdentity identity)
        {
            if (!_activeObjects.ContainsKey(identity.NetID))
            {
                _activeObjects.Add(identity.NetID, identity);
            }
        }

        public void Unregister(int netId)
        {
            if (_activeObjects.ContainsKey(netId))
            {
                _activeObjects.Remove(netId);
            }
        }

        public NetworkIdentity GetIdentity(int netId)
        {
            _activeObjects.TryGetValue(netId, out var identity);
            return identity;
        }

        [MessageHandler(Protocol.RpcID)]
        public void InvokeLocalRPC(RPCMessage msg)
        {
            NetworkIdentity netObj = GetIdentity(msg.NetID);
            if (netObj == null) return; 

            foreach (var behaviour in netObj.GetComponents<NetworkBehaviour>())
            {
                if (behaviour == null) continue;
                if (behaviour.InvokeRPC(msg.MethodName, msg.Parameters))
                {
                    break;
                }
            }
        }
    }
}