

using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 单位标记容器。负责管理一个单位的所有标记（Buff/Debuff/状态效果等）。
    /// </summary>
    /// <remarks>
    /// 记得在单位的 Update 中调用 UpdateMarks 来维护标记的生命周期。
    /// </remarks>
    public class MarkContainer : IUnitTagSource, IEnumerable<KeyValuePair<UnitTag, UnitMark>>
    {
        private readonly Dictionary<UnitTag, UnitMark> _marks = new();
        private List<UnitMark> _markListCache = new(); // 缓存列表，避免频繁创建
        public bool HasTag(UnitTag tag) => _marks.ContainsKey(tag);
        public int Count => _marks.Count;
        public IEnumerator<KeyValuePair<UnitTag, UnitMark>> GetEnumerator() => _marks.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _marks.GetEnumerator();

        /// <summary>
        /// 添加一个标记。如果已存在同名标记，则尝试叠加。
        /// </summary>
        public void AddMark(UnitMark newMark)
        {
            if (_marks.TryGetValue(newMark.Name, out var existingMark))
            {
                existingMark.OnStack(newMark);
            }
            else
            {
                newMark.OnApply();
                _marks[newMark.Name] = newMark;
            }
        }

        /// <summary>
        /// 移除指定标记。
        /// </summary>
        public void RemoveMark(UnitTag tag)
        {
            if (_marks.TryGetValue(tag, out var mark))
            {
                mark.OnRemove();
                _marks.Remove(tag);
            }
        }

        /// <summary>
        /// 获取指定标记实例。
         /// </summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tag"></param>
        /// <returns></returns>
        public T GetMark<T>(UnitTag tag) where T : UnitMark
        {
            return _marks.TryGetValue(tag, out var mark) ? mark as T : null;
        }

        /// <summary>
        /// 更新所有标记的计时器，并移除已过期的标记。
        /// </summary>
        public void UpdateMarks(float deltaTime)
        {
            foreach (var kvp in _marks)
            {
                var mark = kvp.Value;
                mark.OnUpdate(deltaTime);
                if (mark.IsExpired)
                {
                    _markListCache.Add(kvp.Value);
                }
            }
            // 移除过期标记
            foreach (var mark in _markListCache)
            {
                RemoveMark(mark.Name);
            }
            _markListCache.Clear();
        }
        
        public void Clear()
        {
            foreach (var mark in _marks.Values)
            {
                mark.OnRemove();
            }
            _marks.Clear();
        }
    }
}