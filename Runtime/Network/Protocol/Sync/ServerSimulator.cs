using System;

namespace GoveKits.Runtime.Network
{
    internal class ServerSimulator
    {
        public float TickInterval { get; set; }
        private float _accumulateTime = 0f;
        private int _currentTick = 0;

        private readonly ServerCollector _collector;

        // 抛给业务层执行的事件
        public event Action<AllInputPackage> OnLogicTick;
        public event Action<int> OnBroadcastTick;

        public ServerSimulator(ServerCollector collector, float tickInterval)
        {
            _collector = collector;
            TickInterval = tickInterval;
        }

        public void Update(float deltaTime)
        {
            _accumulateTime += deltaTime;
            while (_accumulateTime >= TickInterval)
            {
                _currentTick++;

                // 1. 从 Collector 拿到这 0.05 秒内收集到的所有玩家输入
                var tickData = _collector.ExtractTickData(_currentTick);

                // 2. 抛给外层（服务器游戏脚本）去执行真实物理移动和伤害计算
                OnLogicTick?.Invoke(tickData);

                // 3. 业务层执行完毕后，世界上部分物体肯定被打上了 IsDirty 标记。开始收集并广播。
                OnBroadcastTick?.Invoke(_currentTick);

                _accumulateTime -= TickInterval;
            }
        }
    }
}