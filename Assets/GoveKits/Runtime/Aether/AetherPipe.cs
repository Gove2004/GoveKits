

using System.Collections.Generic;

namespace GoveKits.Aether
{
// ==========================================================
    // 4. AetherPipe: 捕获管道 (封装流逻辑)
    // ==========================================================
    /// <summary>
    /// 某种特定以太的专属流动通道。
    /// 维护了有序的捕获器列表。
    /// </summary>
    public class AetherPipe
    {
        private readonly List<AetherCatcher> _catchers = new List<AetherCatcher>();
        private bool _isDirty = false; // 脏标记，用于延迟排序

        public void Add(AetherCatcher catcher)
        {
            _catchers.Add(catcher);
            _isDirty = true;
        }

        public void Remove(AetherCatcher catcher)
        {
            _catchers.Remove(catcher);
        }

        /// <summary>
        /// 泵送以太流经所有节点
        /// </summary>
        public void Flow(AetherInfo aether)
        {
            if (_catchers.Count == 0) return;

            // 只有在变动后才重新排序，优化性能
            if (_isDirty)
            {
                _catchers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _isDirty = false;
            }

            // 极速遍历
            // 注意：这里使用 for 循环避免 foreach 的 Enumerator GC
            for (int i = 0; i < _catchers.Count; i++)
            {
                _catchers[i].OnFlowIn(aether);
            }
        }
    }
}