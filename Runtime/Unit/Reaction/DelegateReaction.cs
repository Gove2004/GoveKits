using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 基于委托 (Delegate) 的具体反应实现类。
    /// <para>职责：无需手动编写新类，即可在运行时通过代码流式装配一个事件监听器。</para>
    /// </summary>
    public class DelegateReaction<T> : UnitReaction<T> where T : EventData, new()
    {
        private Action<T> _reactionAction;
        private Func<T, bool> _filterFunc;
        
        private UnitTag _name;
        private int _priority;

        public override UnitTag Name => _name;
        public override int Priority => _priority;

        public DelegateReaction() { }

        #region 流式装配接口 (Fluent API)

        public DelegateReaction<T> SetName(UnitTag name)
        {
            _name = name;
            return this;
        }

        public DelegateReaction<T> SetPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>
        /// 注入核心执行逻辑。
        /// </summary>
        public DelegateReaction<T> SetAction(Action<T> reactionAction)
        {
            _reactionAction = reactionAction;
            return this;
        }

        /// <summary>
        /// 注入自定义的事件过滤条件。
        /// </summary>
        public DelegateReaction<T> SetFilter(Func<T, bool> filterFunc)
        {
            _filterFunc = filterFunc;
            return this;
        }

        #endregion

        public override bool OnFilter(T eventInfo)
        {
            if (_filterFunc != null) return _filterFunc.Invoke(eventInfo);
            return base.OnFilter(eventInfo);
        }

        public override void OnEvent(T eventInfo)
        {
            _reactionAction?.Invoke(eventInfo);
        }
    }
}