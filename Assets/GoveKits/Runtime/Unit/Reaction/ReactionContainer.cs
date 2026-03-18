

using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 反应容器。
    /// </summary>
    /// <remarks>
    /// 负责管理反应实例的增删、批量激活/停用以及生命周期清理。
    /// </remarks>
    public class ReactionContainer : IUnitTagSource, IEnumerable<KeyValuePair<UnitTag, UnitReaction>>
    {
        private bool _isActive = false;
        private readonly Dictionary<UnitTag, UnitReaction> _reactions = new();
        public bool HasTag(UnitTag tag) => _reactions.ContainsKey(tag);
        public int Count => _reactions.Count;
        public IEnumerator<KeyValuePair<UnitTag, UnitReaction>> GetEnumerator() => _reactions.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _reactions.GetEnumerator();

        /// <summary>
        /// 设置容器激活状态。
        /// </summary>
        /// <param name="active">true 为激活全部反应，false 为停用全部反应。</param>
        public void SetActive(bool active)
        {
            _isActive = active;
            foreach (var reaction in _reactions.Values)
            {
                if (_isActive)
                {
                    reaction.Activate();
                }
                else
                {
                    reaction.Deactivate();
                }
            }
        }

        /// <summary>
        /// 添加一个反应到容器。
        /// </summary>
        /// <param name="reaction">待添加反应，为 null 时忽略。</param>
        /// <remarks>
        /// 若存在同名反应，会先移除旧实例；
        /// 若容器当前已激活，新反应会立即激活。
        /// </remarks>
        public void AddReaction(UnitReaction reaction)
        {
            if (reaction == null) return;
            if (_reactions.ContainsKey(reaction.Name))
            {
                // 已存在同名反应，先移除旧的
                RemoveReaction(reaction.Name);
            }
            _reactions[reaction.Name] = reaction;
            if (_isActive)
            {
                reaction.Activate();
            }

        }

        /// <summary>
        /// 移除指定名称的反应。
        /// </summary>
        /// <param name="name">反应名称。</param>
        public void RemoveReaction(UnitTag name)
        {
            UnitReaction reaction;
            if (_reactions.TryGetValue(name, out reaction))
            {
                reaction.Dispose();
                _reactions.Remove(name);
            }
        }

        /// <summary>
        /// 获取指定名称的反应实例。
        /// </summary>
        public T GetReaction<T>(UnitTag name) where T : UnitReaction
        {
            return _reactions.TryGetValue(name, out var reaction) ? reaction as T : null;
        }

        /// <summary>
        /// 激活指定反应。
        /// </summary>
        /// <param name="name">反应名称。</param>
        public void ActivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction))
            {
                reaction.Activate();
            }
        }

        /// <summary>
        /// 停用指定反应。
        /// </summary>
        /// <param name="name">反应名称。</param>
        public void DeactivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction))
            {
                reaction.Deactivate();
            }
        }

        /// <summary>
        /// 清空容器并释放所有反应。
        /// </summary>
        public void Clear()
        {
            foreach (var reaction in _reactions.Values)
            {
                reaction.Dispose();
            }
            _reactions.Clear();
        }
    }
}