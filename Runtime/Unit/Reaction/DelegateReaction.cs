using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 基于委托的具体反应实现。
    /// 职责：快速实例化，无需手写新类。
    /// </summary>
    public class DelegateReaction<T> : UnitReaction<T> where T : EventData, new()
    {
        /// <summary>
        /// 反应执行的动作委托
        /// </summary>
        private readonly Action<T> _reactionAction;
        
        // 字段 backing
        private readonly UnitTag _name;
        private readonly int _priority;

        /// <summary>
        /// 获取反应名称
        /// </summary>
        public override UnitTag Name => _name;
        
        /// <summary>
        /// 获取反应优先级
        /// </summary>
        public override int Priority => _priority;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">反应所属单位</param>
        /// <param name="name">反应名称</param>
        /// <param name="reactionAction">反应执行的委托</param>
        /// <param name="priority">反应优先级</param>
        public DelegateReaction(IUnit owner, UnitTag name, Action<T> reactionAction, int priority = 0) 
            : base(owner)
        {
            _name = name;
            _reactionAction = reactionAction;
            _priority = priority;
        }

        /// <summary>
        /// 事件处理方法，执行预设的委托
        /// </summary>
        /// <param name="eventInfo">事件数据</param>
        public override void OnEvent(T eventInfo)
        {
            _reactionAction?.Invoke(eventInfo);
        }
    }
}