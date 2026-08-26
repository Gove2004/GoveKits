using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 基于委托的快捷反应实现类。
    /// 无需手动编写新类，即可通过代码流式装配一个 Intent 处理器。
    /// </summary>
    public class DelegateReaction : UnitReaction
    {
        private UnitTag _name;
        private int _priority;
        private System.Func<UnitIntent, bool> _canHandle;
        private System.Action<UnitIntent, IList<UnitEffect>> _action;

        /// <summary>反应的唯一标识</summary>
        public override UnitTag Name => _name;

        /// <summary>处理优先级</summary>
        public override int Priority => _priority;

        /// <summary>创建一个流式装配的 DelegateReaction</summary>
        public static DelegateReaction Create() => new DelegateReaction();

        private DelegateReaction() { }

        /// <summary>设置反应名称</summary>
        public DelegateReaction SetName(UnitTag name)
        {
            _name = name;
            return this;
        }

        /// <summary>设置处理优先级（值越大越先执行）</summary>
        public DelegateReaction SetPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>设置 Intent 过滤条件。返回 false 则跳过此 Reaction</summary>
        public DelegateReaction SetCanHandle(System.Func<UnitIntent, bool> filter)
        {
            _canHandle = filter;
            return this;
        }

        /// <summary>设置核心处理逻辑。直接产出 Effect</summary>
        public DelegateReaction SetAction(System.Action<UnitIntent, IList<UnitEffect>> action)
        {
            _action = action;
            return this;
        }

        /// <summary>
        /// 检查此 Reaction 是否能够处理给定的 Intent。
        /// 未设置过滤条件时默认返回 true。
        /// </summary>
        public override bool CanHandle(UnitIntent intent)
            => _canHandle == null || _canHandle.Invoke(intent);

        /// <summary>
        /// 处理 Intent，产出 Effect。
        /// </summary>
        public override void Handle(UnitIntent intent, IList<UnitEffect> effects)
            => _action?.Invoke(intent, effects);
    }
}
