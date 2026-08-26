using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 被动反应容器。
    /// 负责管理反应实例的增删，处理委托给 ReactionChain 按优先级执行。
    /// Reaction 是 Intent 的唯一处理者：读取 Intent + Attribute + Mark，产出 Effect。
    /// </summary>
    public class ReactionContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitReaction>>
    {
        /// <summary>反应所属的宿主 Unit</summary>
        public IUnit Owner { get; }
        private readonly Dictionary<UnitTag, UnitReaction> _reactions = new();
        private readonly ReactionChain _chain = new();

        /// <summary>收集 Reaction 产出的 Effect，由 Unit 统一 Apply</summary>
        private readonly List<UnitEffect> _effectBuffer = new();

        /// <summary>当前已注册的反应数量</summary>
        public int Count => _reactions.Count;

        /// <summary>构造函数</summary>
        public ReactionContainer(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>检查是否存在指定标签的反应</summary>
        public bool HasTag(UnitTag tag) => _reactions.ContainsKey(tag);

        public IEnumerator<KeyValuePair<UnitTag, UnitReaction>> GetEnumerator() => _reactions.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _reactions.GetEnumerator();

        /// <summary>
        /// 添加一个反应到容器，自动注入宿主并激活。
        /// 若已存在同名反应，先移除旧实例以避免重复。
        /// </summary>
        public void AddReaction(UnitReaction reaction)
        {
            if (reaction == null) return;

            if (_reactions.ContainsKey(reaction.Name))
            {
                RemoveReaction(reaction.Name);
            }

            reaction.Init(Owner);
            _reactions[reaction.Name] = reaction;
            _chain.Add(reaction);
            _chain.Refresh();

            reaction.Activate();
        }

        /// <summary>移除指定标签的反应</summary>
        public void RemoveReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction))
            {
                reaction.Dispose();
                _reactions.Remove(name);
                _chain.Remove(reaction);
                _chain.Refresh();
            }
        }

        /// <summary>强类型获取指定标签的反应实例</summary>
        public T GetReaction<T>(UnitTag name) where T : UnitReaction
        {
            return _reactions.TryGetValue(name, out var reaction) ? reaction as T : null;
        }

        /// <summary>单独唤醒某个处于沉睡状态的特殊反应</summary>
        public void ActivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction)) reaction.Activate();
        }

        /// <summary>单独封印某个反应</summary>
        public void DeactivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction)) reaction.Deactivate();
        }

        /// <summary>启用或禁用指定反应</summary>
        public void Enable(UnitTag reactionTag, bool enable)
        {
            if (enable) ActivateReaction(reactionTag);
            else DeactivateReaction(reactionTag);
        }

        /// <summary>
        /// 处理 Intent：委托给 ReactionChain 按优先级分发，收集产出的 Effect。
        /// </summary>
        public void Handle(UnitIntent intent)
        {
            _effectBuffer.Clear();
            _chain.Handle(intent, _effectBuffer);

            if (_effectBuffer.Count > 0)
            {
                ApplyEffects();
            }
        }

        /// <summary>
        /// 将所有 Reaction 产出的 Effect 应用到 Unit 状态。
        /// </summary>
        private void ApplyEffects()
        {
            int count = _effectBuffer.Count;
            for (int i = 0; i < count; i++)
            {
                var effect = _effectBuffer[i];
                if (effect != null)
                {
                    effect.Apply(Owner);
                    _effectBuffer[i] = null;
                }
            }
            _effectBuffer.Clear();
        }

        /// <summary>获取当前缓冲的 Effect 数量（调试用）</summary>
        public int EffectCount => _effectBuffer.Count;

        /// <summary>销毁并清空所有反应实例</summary>
        public void Clear()
        {
            foreach (var reaction in _reactions.Values)
            {
                reaction.Dispose();
            }
            _reactions.Clear();
            _chain.Clear();
            _effectBuffer.Clear();
        }
    }
}
