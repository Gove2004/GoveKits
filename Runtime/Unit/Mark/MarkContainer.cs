using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 单位标记容器。负责托管该单位身上所有的标记（Buff/Debuff/护盾等状态）。
    /// </summary>
    public class MarkContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitMark>>
    {
        public IUnit Owner { get; }
        private readonly Dictionary<UnitTag, UnitMark> _marks = new();
        
        public int Count => _marks.Count;

        // 缓存迭代队列，解决游戏循环中经典的“在遍历字典时添加/移除元素导致异常”问题
        private readonly List<UnitMark> _updateListCache = new(); 
        private readonly List<UnitMark> _expiredMarkCache = new(); 

        public MarkContainer(IUnit owner)
        {
            Owner = owner;
        }
        
        public bool HasTag(UnitTag tag) => _marks.ContainsKey(tag);
        
        public IEnumerator<KeyValuePair<UnitTag, UnitMark>> GetEnumerator() => _marks.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _marks.GetEnumerator();

        /// <summary>
        /// 挂载一个新的状态标记。
        /// </summary>
        public void AddMark(UnitMark newMark)
        {
            if (newMark == null) return;

            if (_marks.TryGetValue(newMark.Name, out var existingMark))
            {
                // 已存在则触发堆叠融合逻辑，废弃新实例
                existingMark.OnStack(newMark);
            }
            else
            {
                // 全新挂载则注入宿主灵魂，派发首次触发事件
                newMark.Init(Owner);
                newMark.OnApply();
                _marks[newMark.Name] = newMark;
            }
        }

        public void RemoveMark(UnitTag tag)
        {
            if (_marks.TryGetValue(tag, out var mark))
            {
                mark.OnRemove();
                _marks.Remove(tag);
            }
        }

        public T GetMark<T>(UnitTag tag) where T : UnitMark
        {
            return _marks.TryGetValue(tag, out var mark) ? mark as T : null;
        }

        /// <summary>
        /// 驱动所有标记的计时器，通常由 UnitBehaviour 的 Update 方法调用。
        /// </summary>
        public void UpdateMarks(float deltaTime)
        {
            // 1. 将当前帧字典拍平到缓存列表中。
            // 这样能防止 OnUpdate 逻辑内部生成新 Mark 污染遍历队列。
            _updateListCache.Clear();
            _updateListCache.AddRange(_marks.Values);

            // 2. 依次迭代更新
            foreach (var mark in _updateListCache)
            {
                // 如果在上一个循环中，某个技能强行终结了该标记，直接跳过
                if (mark.IsExpired) continue; 
                
                mark.OnUpdate(deltaTime);
                
                if (mark.IsExpired)
                {
                    _expiredMarkCache.Add(mark);
                }
            }
            
            // 3. 集中处刑所有自然死亡的过期标记
            if (_expiredMarkCache.Count > 0)
            {
                foreach (var mark in _expiredMarkCache) 
                { 
                    RemoveMark(mark.Name); 
                }
                _expiredMarkCache.Clear();
            }
        }
        
        public void Clear()
        {
            foreach (var mark in _marks.Values)
            {
                mark.OnRemove();
            }
            _marks.Clear();
            _updateListCache.Clear();
            _expiredMarkCache.Clear();
        }
    }
}