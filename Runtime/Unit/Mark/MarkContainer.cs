using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 单位标记容器。负责托管该单位身上所有的标记（Buff/Debuff/护盾等状态）。
    /// 支持标记叠加、过期自动移除，并提供线程安全的迭代更新。
    /// </summary>
    public class MarkContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitMark>>
    {
        /// <summary>标记所属的宿主 Unit</summary>
        public IUnit Owner { get; }
        private readonly Dictionary<UnitTag, UnitMark> _marks = new();

        /// <summary>当前已注册的标记数量</summary>
        public int Count => _marks.Count;

        // 缓存迭代队列，解决游戏循环中经典的"在遍历时添加/移除元素"问题
        private readonly List<UnitMark> _updateListCache = new();
        private readonly List<UnitMark> _expiredMarkCache = new();

        /// <summary>构造函数（依赖注入）</summary>
        public MarkContainer(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>检查是否存在指定标签的标记</summary>
        public bool HasTag(UnitTag tag) => _marks.ContainsKey(tag);

        public IEnumerator<KeyValuePair<UnitTag, UnitMark>> GetEnumerator() => _marks.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _marks.GetEnumerator();

        /// <summary>
        /// 挂载一个新的状态标记。
        /// 若已存在同名标记则触发 OnStack 叠加逻辑，否则初始化并触发 OnApply。
        /// </summary>
        public void AddMark(UnitMark newMark)
        {
            if (newMark == null) return;

            if (_marks.TryGetValue(newMark.Name, out var existingMark))
            {
                existingMark.OnStack(newMark);
            }
            else
            {
                newMark.Init(Owner);
                newMark.OnApply();
                _marks[newMark.Name] = newMark;
            }
        }

        /// <summary>移除指定标签的标记</summary>
        public void RemoveMark(UnitTag tag)
        {
            if (_marks.TryGetValue(tag, out var mark))
            {
                mark.OnRemove();
                _marks.Remove(tag);
            }
        }

        /// <summary>强类型获取指定标签的标记实例</summary>
        public T GetMark<T>(UnitTag tag) where T : UnitMark
        {
            return _marks.TryGetValue(tag, out var mark) ? mark as T : null;
        }

        /// <summary>
        /// 驱动所有标记的计时器，通常由 UnitBehaviour 的 Update 方法调用。
        /// 过期标记会在本帧末尾统一移除。
        /// </summary>
        public void UpdateMarks(float deltaTime)
        {
            _updateListCache.Clear();
            _updateListCache.AddRange(_marks.Values);

            foreach (var mark in _updateListCache)
            {
                if (mark.IsExpired) continue;
                mark.OnUpdate(deltaTime);

                if (mark.IsExpired)
                {
                    _expiredMarkCache.Add(mark);
                }
            }

            if (_expiredMarkCache.Count > 0)
            {
                foreach (var mark in _expiredMarkCache)
                {
                    // 移除前二次校验：OnUpdate/OnTick 期间标记可能被 OnStack 叠层复活
                    if (!mark.IsExpired) continue;
                    RemoveMark(mark.Name);
                }
                _expiredMarkCache.Clear();
            }
        }

        /// <summary>销毁并清空所有标记</summary>
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
