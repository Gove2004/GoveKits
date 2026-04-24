using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Network
{
    internal class ClientStateLerper
    {
        private readonly List<WorldPackage> _snapshots = new List<WorldPackage>();
        
        // 表现延迟：故意让客户端慢 100 毫秒，凑齐前后的快照进行完美插值
        public float InterpolationDelay { get; set; } = 0.1f; 
        private float _renderTime = 0f;
        
        // 抛给外界的插值事件
        public Action<WorldPackage, WorldPackage, float> OnInterpolationTick;

        public void EnqueueSnapshot(WorldPackage snapshot)
        {
            _snapshots.Add(snapshot);
            
            // 限制缓冲区大小
            if (_snapshots.Count > 10) _snapshots.RemoveAt(0);

            // 以最新的服务器时间粗略校准本地渲染时间
            if (_snapshots.Count == 1) 
                _renderTime = snapshot.ServerTime - InterpolationDelay;
        }

        public void Update(float deltaTime)
        {
            if (_snapshots.Count < 2) return;

            // 推进本地时间
            _renderTime += deltaTime;

            WorldPackage from = null;
            WorldPackage to = null;

            // 寻找包含 renderTime 的前后两个快照
            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                if (_snapshots[i].ServerTime <= _renderTime && _snapshots[i+1].ServerTime >= _renderTime)
                {
                    from = _snapshots[i];
                    to = _snapshots[i+1];
                    break;
                }
            }

            if (from != null && to != null)
            {
                // 计算 0~1 的插值比率
                float t = (_renderTime - from.ServerTime) / (to.ServerTime - from.ServerTime);
                
                // 驱动表现层平滑移动
                OnInterpolationTick?.Invoke(from, to, t);
            }
        }
    }
}