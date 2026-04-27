using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class SyncCore
    {
        // 客户端组件
        private static ClientInputor _clientSubmitor;
        private static ClientLerper _clientLerper;
        
        // 服务端组件
        private static ServerCollector _serverCollector;
        private static ServerSimulator _serverSimulator;

        private static readonly SyncProxy _proxy = new SyncProxy();

        
        // 2. 服务端业务层要监听这个事件，来推演世界
        public static event Action<AllInputPackage, float> OnServerLogicTick;

        public static void StartClient(float submitInterval = 0.05f, float serverTickRate = 0.05f)
        {
            _clientSubmitor = new ClientInputor(submitInterval);
            _clientLerper = new ClientLerper(serverTickRate);

            _clientSubmitor.OnSubmit += ClientSubmitInput;
            
            ClientCore.Dispatcher.Bind(_proxy);
        }

        public static void StartServer(float tickInterval = 0.05f)
        {
            _serverCollector = new ServerCollector();
            _serverSimulator = new ServerSimulator(_serverCollector, tickInterval);
            
            _serverSimulator.OnLogicTick += (data) => OnServerLogicTick?.Invoke(data, tickInterval);
            _serverSimulator.OnBroadcastTick += (currentTick) => ServerBroadcastStates(currentTick);

            ServerCore.Dispatcher.Bind(_proxy);
            ServerCore.OnClientConnected += ServerSendToNewClient;
        }

        public static void Stop()
        {
            _clientSubmitor = null;
            _clientLerper = null;
            _serverCollector = null;
            _serverSimulator = null;
            
            ClientCore.Dispatcher.Unbind(_proxy);
            ServerCore.Dispatcher.Unbind(_proxy);
        }

        public static void Update(float deltaTime)
        {
            // 客户端：收集并发送输入 -> 接收快照并插值
            _clientSubmitor?.Update(deltaTime);
            _clientLerper?.Update(deltaTime);

            // 服务端：定时模拟并广播
            _serverSimulator?.Update(deltaTime);
        }







        private static void ClientSubmitInput(Type inputType, object inputData)
        {
            // 1. 把输入数据打包成网络消息
            var msg = new PlayerInputPackage
            {
                PlayerId = ClientCore.PlayerId,
                Payload = ProtocolCore.Serialize(inputType, inputData)
            };

            // 2. 发送给服务器
            ClientCore.Send(msg);
        }

        private static void ServerBroadcastStates(int currentTick)
        {
            var changedList = new List<EntityState>();

            // 依赖 SpawnCore 获取所有实体
            var allEntities = SpawnCore.GetAllEntities();

            foreach (var entity in allEntities)
            {
                if (entity is ISyncable syncable && syncable.IsDirty)
                {
                    changedList.Add(new EntityState
                    {
                        NetId = syncable.NetId,
                        StatePayload = syncable.GetState().Item2
                    });

                    // 数据采集完毕，清除实体的脏标记
                    syncable.IsDirty = false; // 【极其重要】千万别忘了清除脏标记！不然下一帧就全发了！
                }
            }

            // 【增量优化】只有在世界上真的有东西发生变化时，才发送网络包！
            if (changedList.Count > 0)
            {
                ServerCore.Broadcast(new WorldPackage
                {
                    Tick = currentTick,
                    Entities = changedList.ToArray()
                }, ClientCore.PlayerId); // 注意：主机直接行动
            }
        }

        public static void ServerSendToNewClient(int clientId)
        {
            foreach (var kv in _spawns)
            {
                uint objectId = kv.Key;
                string spawnKey = kv.Value;

                // 直接从 SpawnCore 里拿实体，拿到什么类型就发什么类型，完全不需要额外的注册表了！
                var entity = SpawnCore.GetEntity(objectId) as ISyncable;
                if (entity == null) continue;

                var (dataType, payload) = entity.GetState();
                ServerCore.SendTo(clientId, new SpawnMsg
                {
                    SpawnKey = spawnKey,
                    ObjectId = objectId,
                    // ISyncable.GetState 已返回序列化后的 payload，这里不能再次按 dataType 序列化。
                    SpawnData = payload,
                    SpawnDataType = dataType
                });
            }
        }
        




        // 客户端调用：修改当前待提交的输入数据
        public static void ClientModifyInput<T>(Action<T> modifier) where T : new()
        {
            _clientSubmitor?.ModifyInput(modifier);
        }




        // 当前存活的实体 ID 和它们对应的 SpawnKey（仅服务端维护，纯客机不维护这个表）
        private static Dictionary<uint, string> _spawns = new();

        /// <summary>
        /// 服务端权威生成！
        /// 无论是主机还是专用服务器，想刷怪/刷子弹，全调这个方法！
        /// </summary>
        public static ISpawnable NetworkSpawn(string spawnKey, ISpawnData data = null)
        {
            if (!ClientCore.IsHost)
            {
                LogCore.Warning(nameof(SyncCore), "Only server can spawn entities!");
                return null;
            }
            
            // 1. 服务端在本地真实生成（Host模式下，这直接就在画面上出来了）
            var entity = SpawnCore.Spawn(spawnKey, data, 0); 

            if (entity != null)
            {
                _spawns[entity.ObjectId] = spawnKey; // 记录 ID 和 SpawnKey 的对应关系，方便后续广播和调试

                // 2. 将生成事件广播给所有客机
                ServerCore.Broadcast(new SpawnMsg
                {
                    SpawnKey = spawnKey,
                    ObjectId = entity.ObjectId,
                    // 假设这里你写了一个序列化数据的方法
                    SpawnData = data == null ? null : ProtocolCore.Serialize(data.GetType(), data),
                    SpawnDataType = data?.GetType()
                });
            }
            return entity;
        }

        /// <summary>
        /// 服务端权威销毁！
        /// </summary>
        public static void NetworkDespawn(uint objectId)
        {
            if (!ClientCore.IsHost)
            {
                LogCore.Warning(nameof(SyncCore), "Only server can despawn entities!");
                return;
            }
            _spawns.Remove(objectId);

            SpawnCore.Despawn(objectId);
            ServerCore.Broadcast(new DespawnMsg(objectId));
        }




        // 内部网络代理
        private class SyncProxy
        {
            // 服务端接收客户端输入
            [MessageHandler]
            public void OnReceiveInput(Session session, PlayerInputPackage msg) => _serverCollector?.OnReceiveInput(msg);

            // 客户端接收服务端广播
            [MessageHandler]
            public void OnReceiveSnapshot(WorldPackage msg)
            {
                if (ClientCore.IsHost) return; 
                _clientLerper?.EnqueueSnapshot(msg);
            }

            [MessageHandler]
            public void OnReceiveSpawn(SpawnMsg msg)
            {
                // 因为 Host 既是 Server 也是 Client。
                // 如果本地 SpawnCore 已经有这个 ID 了，说明是刚才 Host Server 刷的，
                // 作为 Host Client，直接无视这条广播！避免报黄字警告！
                if (SpawnCore.GetEntity(msg.ObjectId) != null) return;

                // 如果走到这里，说明是纯客机 (Pure Client)。
                // 听从服务器的指挥，拿着服务器分配好的 ID 强行生成！
                ISpawnData data = ProtocolCore.Deserialize(msg.SpawnDataType, msg.SpawnData) as ISpawnData;
                SpawnCore.Spawn(msg.SpawnKey, data, msg.ObjectId);
            }

            [MessageHandler]
            public void OnReceiveDespawn(DespawnMsg msg)
            {
                // 同样，如果是 Host，在 NetworkDespawn 时已经销毁过了，这里 Despawn 不会报错，但也可以提前 return
                if (SpawnCore.GetEntity(msg.ObjectId) == null) return;
                SpawnCore.Despawn(msg.ObjectId);
            }
        }
    }
}