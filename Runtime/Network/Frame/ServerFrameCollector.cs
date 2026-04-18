using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Runtime.Network
{
    internal class ServerFrameCollector
    {
        public int CurrentFrameId { get; private set; } = 1;
        public float TickInterval { get; set; }

        private float _accumulateTime = 0f;
        private readonly Dictionary<int, FrameInputPackage> _currentInputs = new();
        private readonly ServerFrameStoreger _storeger;

        public ServerFrameCollector(ServerFrameStoreger storeger)
        {
            _storeger = storeger;
        }

        public void CollectInput(FrameInputPackage input)
        {
            _currentInputs[input.PlayerId] = input; // 覆盖保存该玩家最新指令
        }

        public void Update(float deltaTime)
        {
            _accumulateTime += deltaTime;
            while (_accumulateTime >= TickInterval)
            {
                var package = new FramePackage
                {
                    FrameId = CurrentFrameId,
                    Inputs = _currentInputs.Values.ToArray()
                };

                // 1. 存入历史库
                _storeger.AppendFrame(package);

                // 2. 广播给所有人
                ServerCore.Broadcast(package);

                // 3. 为下一帧准备
                _currentInputs.Clear();
                CurrentFrameId++;
                _accumulateTime -= TickInterval;
            }
        }
    }
}