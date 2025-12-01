



using System;
using System.Collections.Generic;

namespace GoveKits.Aether
{
    // ==========================================================
    // 5. AetherPlane: 以太位面 (独立世界)
    // ==========================================================
    /// <summary>
    /// 一个独立的逻辑容器。不同位面之间的以太互不干扰。
    /// </summary>
    public class AetherPlane
    {
        public string Name { get; private set; }
        
        // 管道字典：Key=以太类型
        private readonly Dictionary<Type, AetherPipe> _pipes = new Dictionary<Type, AetherPipe>();

        public AetherPlane(string name) { Name = name; }

        /// <summary>
        /// 部署捕获器
        /// </summary>
        public void Deploy<T>(AetherCatcher<T> catcher) where T : AetherInfo
        {
            var type = typeof(T);
            if (!_pipes.ContainsKey(type)) _pipes[type] = new AetherPipe();
            
            _pipes[type].Add(catcher);
        }

        /// <summary>
        /// 撤收捕获器 (动态移除)
        /// </summary>
        public void Withdraw<T>(AetherCatcher<T> catcher) where T : AetherInfo
        {
            var type = typeof(T);
            if (_pipes.TryGetValue(type, out var pipe))
            {
                pipe.Remove(catcher);
            }
        }

        /// <summary>
        /// 内部泵送入口
        /// </summary>
        internal void PumpInternal(AetherInfo aether)
        {
            if (_pipes.TryGetValue(aether.GetType(), out var pipe))
            {
                pipe.Flow(aether);
            }
        }
    }
}