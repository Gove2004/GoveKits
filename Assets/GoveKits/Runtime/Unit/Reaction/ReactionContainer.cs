

using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 反应容器。
    /// </summary>
    /// <remarks>
    /// 负责管理反应实例的增删、批量激活/停用以及生命周期清理。
    /// </remarks>
    public class ReactionContainer : IUnitTagSource
    {
        /// <summary>
        /// 反应字典，Key 为反应标签。
        /// </summary>
        private readonly Dictionary<UnitTag, IUnitReaction> _reactions = new();

        /// <summary>
        /// 容器当前是否处于激活状态。
        /// </summary>
        private bool _isActive = false;

        /// <summary>
        /// 判断容器内是否存在指定标签的反应。
        /// </summary>
        /// <param name="tag">反应标签。</param>
        /// <returns>存在返回 true，否则返回 false。</returns>
        public bool HasTag(UnitTag tag) => _reactions.ContainsKey(tag);

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
        public void AddReaction(IUnitReaction reaction)
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
            IUnitReaction reaction;
            if (_reactions.TryGetValue(name, out reaction))
            {
                reaction.Dispose();
                _reactions.Remove(name);
            }
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