using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Runtime.Network
{
    internal class ServerStateSimulator
    {
        public float TickInterval { get; set; } // 服务端广播快照频率 (通常是 0.05f 即 20Hz)
        
        private float _accumulateTime = 0f;
        private int _currentTick = 1;
        private float _serverTime = 0f;

        // 收集客户端指令
        private readonly Dictionary<int, StateInputPackage> _clientInputs = new();
        // 缓存服务端下发的世界实体状态
        private readonly Dictionary<int, EntityState> _entityStates = new();

        public Action<List<StateInputPackage>> OnServerLogicTick;

        public void CollectInput(StateInputPackage input)
        {
            _clientInputs[input.PlayerId] = input; // 覆盖保留最新意图
        }

        public void UpdateEntityState(int netId, float px, float py, float pz, float rx, float ry, float rz, byte[] payload)
        {
            _entityStates[netId] = new EntityState
            {
                NetId = netId,
                Px = px, Py = py, Pz = pz,
                Rx = rx, Ry = ry, Rz = rz,
                Payload = payload
            };
        }

        public void Update(float deltaTime)
        {
            _serverTime += deltaTime;
            _accumulateTime += deltaTime;

            while (_accumulateTime >= TickInterval)
            {
                // 1. 触发服务端业务层推演 (物理、逻辑)
                OnServerLogicTick?.Invoke(_clientInputs.Values.ToList());

                // 2. 打包世界快照
                var snapshot = new WorldPackage
                {
                    SnapshotTick = _currentTick,
                    ServerTime = _serverTime,
                    Entities = _entityStates.Values.ToArray()
                };

                // 3. 广播给所有人
                ServerCore.Broadcast(snapshot);

                // 4. 清理环境，进入下一帧
                _clientInputs.Clear();
                _currentTick++;
                _accumulateTime -= TickInterval;
            }
        }
    }
}