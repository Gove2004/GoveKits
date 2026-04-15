using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 被动反应容器。
    /// </summary>
    /// <remarks>
    /// 负责管理反应实例的增删、批量激活/停用以及生命周期清理。
    /// 容器接管了所有 Reaction 实例的 Owner 依赖注入权。
    /// </remarks>
    public class ReactionContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitReaction>>
    {
        public IUnit Owner { get; }
        private readonly Dictionary<UnitTag, UnitReaction> _reactions = new();

        public int Count => _reactions.Count;

        /// <summary>构造函数，绑定宿主单位</summary>
        public ReactionContainer(IUnit owner)
        {
            Owner = owner;
        }
        
        public bool HasTag(UnitTag tag) => _reactions.ContainsKey(tag);
        
        public IEnumerator<KeyValuePair<UnitTag, UnitReaction>> GetEnumerator() => _reactions.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _reactions.GetEnumerator();

        /// <summary>
        /// 添加一个反应到容器，自动注入宿主。
        /// 若容器当前处于激活状态，新反应会立即激活并开始监听。
        /// </summary>
        public void AddReaction(UnitReaction reaction)
        {
            if (reaction == null) return;
            
            if (_reactions.ContainsKey(reaction.Name))
            {
                RemoveReaction(reaction.Name);
            }

            // 【核心注入机制】赋予其 Owner 灵魂
            reaction.Init(Owner);
            _reactions[reaction.Name] = reaction;
            
            reaction.Activate();
        }

        public void RemoveReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction))
            {
                reaction.Dispose(); // 内部会自动 Deactivate 取消订阅
                _reactions.Remove(name);
            }
        }

        public T GetReaction<T>(UnitTag name) where T : UnitReaction
        {
            return _reactions.TryGetValue(name, out var reaction) ? reaction as T : null;
        }

        /// <summary>单独唤醒某个处于沉睡状态的特殊反应</summary>
        public void ActivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction)) reaction.Activate();
        }

        /// <summary>单独封印某个过于强大的特殊反应（如：被缴械时封印武器格挡被动）</summary>
        public void DeactivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction)) reaction.Deactivate();
        }

        public void Enable(UnitTag reactionTag, bool enable)
        {
            if (enable) ActivateReaction(reactionTag);
            else DeactivateReaction(reactionTag);
        }

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