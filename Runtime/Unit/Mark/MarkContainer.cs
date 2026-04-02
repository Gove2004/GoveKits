using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 单位标记容器。负责管理一个单位的所有标记（Buff/Debuff/状态效果等）。
    /// </summary>
    /// <remarks>
    /// 记得在单位的 Update 中调用 UpdateMarks 来维护标记的生命周期。
    /// </remarks>
    public class MarkContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitMark>>
    {
        // 存储标记的字典，以标记名称为键，标记实例为值
        private readonly Dictionary<UnitTag, UnitMark> _marks = new();
        
        // 缓存列表，用于避免在UpdateMarks方法中频繁创建临时列表
        private List<UnitMark> _markListCache = new(); 
        
        /// <summary>
        /// 检查是否包含指定标签的标记
        /// </summary>
        public bool HasTag(UnitTag tag) => _marks.ContainsKey(tag);
        
        /// <summary>
        /// 标记数量
        /// </summary>
        public int Count => _marks.Count;
        
        /// <summary>
        /// 获取枚举器，支持foreach遍历
        /// </summary>
        public IEnumerator<KeyValuePair<UnitTag, UnitMark>> GetEnumerator() => _marks.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _marks.GetEnumerator();

        /// <summary>
        /// 添加一个标记。如果已存在同名标记，则尝试叠加。
        /// </summary>
        public void AddMark(UnitMark newMark)
        {
            if (_marks.TryGetValue(newMark.Name, out var existingMark))
            {
                // 如果存在同名标记，调用OnStack方法进行堆叠处理
                existingMark.OnStack(newMark);
            }
            else
            {
                // 否则，调用OnApply方法应用新标记，并将其添加到容器中
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
                // 调用OnRemove方法进行清理
                mark.OnRemove();
                _marks.Remove(tag);
            }
        }

        /// <summary>
        /// 获取指定标记实例。
        /// </summary>
        /// <typeparam name="T">期望的标记类型</typeparam>
        /// <param name="tag">标记标签</param>
        /// <returns>标记实例，如果不存在或类型不匹配则返回null</returns>
        public T GetMark<T>(UnitTag tag) where T : UnitMark
        {
            return _marks.TryGetValue(tag, out var mark) ? mark as T : null;
        }

        private readonly List<UnitMark> _updateListCache = new(); // 迭代专用缓存
        /// <summary>
        /// 更新所有标记的计时器，并移除已过期的标记。
        /// </summary>
        public void UpdateMarks(float deltaTime)
        {
            _updateListCache.Clear();
           _updateListCache.AddRange(_marks.Values);
            // 1. 将当前帧的所有 Mark 拍平到一个列表里
            _updateListCache.Clear();
            _updateListCache.AddRange(_marks.Values);

            // 2. 迭代列表（即使内部触发逻辑导致了新 Mark 插入字典，也不会报错）
            foreach (var mark in _updateListCache)
            {
                if (mark.IsExpired) continue; // 如果在迭代中途已经过期，跳过
                mark.OnUpdate(deltaTime);
                if (mark.IsExpired)
                {
                    _markListCache.Add(mark);
                }
            }
            
            // 3. 清理过期
            foreach (var mark in _markListCache) { RemoveMark(mark.Name); }
            _markListCache.Clear();
        }
        
        /// <summary>
        /// 清空所有标记
        /// </summary>
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