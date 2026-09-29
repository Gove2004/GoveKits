using System.Collections.Generic;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 被动反应容器。
    /// 只管反应实例的增删、宿主注入与激活开关；
    /// 事件的分发交由全局 EventCore 完成，容器不参与转发。
    ///
    /// 用法：
    /// <code>
    /// unit.Reactions.AddReaction(UnitCore.CreateReaction&lt;ThornsReaction&gt;());  // 挂载即激活（开始监听）
    /// unit.Reactions.Enable("passive_thorns", false);                            // 暂时封印
    /// unit.Reactions.RemoveReaction("passive_thorns");                           // 卸载并注销监听
    /// </code>
    /// </summary>
    public class ReactionContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitReaction>>
    {
        /// <summary>反应所属的宿主 Unit</summary>
        public IUnit Owner { get; }

        private readonly Dictionary<UnitTag, UnitReaction> _reactions = new();

        /// <summary>当前已挂载的反应数量</summary>
        public int Count => _reactions.Count;

        /// <summary>构造函数，绑定宿主单位</summary>
        public ReactionContainer(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>检查是否存在指定标签的反应</summary>
        public bool HasTag(UnitTag tag) => _reactions.ContainsKey(tag);

        public IEnumerator<KeyValuePair<UnitTag, UnitReaction>> GetEnumerator() => _reactions.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _reactions.GetEnumerator();

        /// <summary>
        /// 挂载一个反应：注入宿主并立即激活（开始监听事件）。
        /// 若已存在同名反应，旧实例会先被卸载，避免重复监听。
        /// </summary>
        public void AddReaction(UnitReaction reaction)
        {
            if (reaction == null) return;

            if (_reactions.ContainsKey(reaction.Name))
            {
                RemoveReaction(reaction.Name);
            }

            // 【核心注入机制】赋予其 Owner
            reaction.Init(Owner);
            _reactions[reaction.Name] = reaction;

            reaction.Activate();
        }

        /// <summary>卸载指定标签的反应，内部会自动注销事件订阅</summary>
        public void RemoveReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction))
            {
                reaction.Dispose();
                _reactions.Remove(name);
            }
        }

        /// <summary>强类型获取指定标签的反应实例，不存在或类型不符返回 null</summary>
        public T GetReaction<T>(UnitTag name) where T : UnitReaction
        {
            return _reactions.TryGetValue(name, out var reaction) ? reaction as T : null;
        }

        /// <summary>单独唤醒某个处于封印状态的反应（重新订阅事件）</summary>
        public void ActivateReaction(UnitTag name)
        {
            if (_reactions.TryGetValue(name, out var reaction)) reaction.Activate();
        }

        /// <summary>单独封印某个反应（注销订阅，如被缴械时封印武器格挡被动）</summary>
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

        /// <summary>卸载并清空所有反应实例</summary>
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
