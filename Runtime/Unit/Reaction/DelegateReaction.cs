using System;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 基于委托的快捷反应实现类。
    /// 无需新建类型，直接在代码里流式装配一个事件监听被动。
    ///
    /// 用法：
    /// <code>
    /// var thorns = new DelegateReaction&lt;DamageEvent&gt;()
    ///     .SetName("passive_thorns")
    ///     .SetPriority(10)                                    // 越大越先执行
    ///     .SetFilter(e => e.Target == unit)                    // 只处理打自己的事件
    ///     .SetAction(e =&gt; AttributeChangeEffect.Create()
    ///                             .Set("hp", -10f).Apply(e.Source));   // 反弹 10 点伤害
    ///
    /// unit.Reactions.AddReaction(thorns);
    /// </code>
    /// 需要携带状态、多字段配置或复用时，仍建议写成 <see cref="UnitReaction{T}"/> 子类。
    /// </summary>
    /// <typeparam name="T">监听的事件类型</typeparam>
    public class DelegateReaction<T> : UnitReaction<T> where T : EventData, new()
    {
        private UnitTag _name;
        private int _priority;
        private Func<T, bool> _filterFunc;
        private Action<T> _reactionAction;

        /// <summary>反应标识，由 <see cref="SetName"/> 指定</summary>
        public override UnitTag Name => _name;

        /// <summary>执行优先级，由 <see cref="SetPriority"/> 指定，默认 0</summary>
        public override int Priority => _priority;

        public DelegateReaction() { }

        #region 流式装配接口 (Fluent API)

        /// <summary>设置反应标识（必填，容器以它作为键）</summary>
        public DelegateReaction<T> SetName(UnitTag name)
        {
            _name = name;
            return this;
        }

        /// <summary>设置优先级，值越大越先执行；不设置则为 0</summary>
        public DelegateReaction<T> SetPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>设置事件过滤条件，返回 false 时本次事件不进入 SetAction 逻辑</summary>
        public DelegateReaction<T> SetFilter(Func<T, bool> filterFunc)
        {
            _filterFunc = filterFunc;
            return this;
        }

        /// <summary>设置事件处理逻辑，在此产出 Effect</summary>
        public DelegateReaction<T> SetAction(Action<T> reactionAction)
        {
            _reactionAction = reactionAction;
            return this;
        }

        #endregion

        /// <summary>未设置过滤器时全放行，否则按过滤器结果决定</summary>
        public override bool OnFilter(T eventData)
        {
            if (_filterFunc != null) return _filterFunc.Invoke(eventData);
            return base.OnFilter(eventData);
        }

        /// <summary>执行装配时注入的处理逻辑</summary>
        public override void OnEvent(T eventData)
        {
            _reactionAction?.Invoke(eventData);
        }
    }
}
