using System;
using GoveKits.Runtime.Core.Event;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 反应接口。
    /// </summary>
    /// <remarks>
    /// Reaction 常用于实现被动技能：监听某类事件并在命中条件时执行响应逻辑。
    /// 生命周期由容器驱动，建议通过 <see cref="ReactionContainer"/> 统一管理。
    /// </remarks>
    public interface IUnitReaction : IEventListener<EventInfo>, IDisposable
    {
        /// <summary>
        /// 反应唯一标识。
        /// </summary>
        UnitTag Name { get; }

        /// <summary>
        /// 当前反应归属的 Unit。
        /// </summary>
        IUnit Owner { get; }

        /// <summary>
        /// 当前反应是否处于激活状态。
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 激活反应并开始监听事件。
        /// </summary>
        void Activate();

        /// <summary>
        /// 停用反应并取消事件监听。
        /// </summary>
        void Deactivate();
    }


    /// <summary>
    /// Unit 反应基类。
    /// </summary>
    /// <typeparam name="T">该反应监听的事件类型。</typeparam>
    /// <remarks>
    /// 该基类内置了订阅句柄管理和激活幂等控制：
    /// 重复 <see cref="Activate"/> 不会重复订阅，重复 <see cref="Deactivate"/> 不会抛错。
    /// </remarks>
    public abstract class UnitReaction<T> : IUnitReaction where T : EventInfo, new()
    {
        /// <summary>
        /// 反应唯一标识，由派生类定义。
        /// </summary>
        public abstract UnitTag Name { get; }

        /// <summary>
        /// 当前反应归属的 Unit。
        /// </summary>
        public IUnit Owner { get; private set; }

        /// <summary>
        /// 当前反应是否已激活。
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 事件优先级，数值越大越先执行。
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// 订阅返回的反订阅句柄。
        /// </summary>
        private DisposeAction _unsubscribeAction;

        /// <summary>
        /// 创建一个反应实例。
        /// </summary>
        /// <param name="owner">归属 Unit。</param>
        public UnitReaction(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 激活反应并订阅对应事件。
        /// </summary>
        public virtual void Activate()
        {
            if (IsActive) return;
            _unsubscribeAction = EventCore.Subscribe<T>(OnEvent, Priority);
            IsActive = true;
        }

        /// <summary>
        /// 停用反应并取消订阅。
        /// </summary>
        public virtual void Deactivate()
        {
            if (!IsActive) return;
            _unsubscribeAction?.Dispose();
            _unsubscribeAction = null;
            IsActive = false;
        }

        /// <summary>
        /// 事件系统回调入口。
        /// </summary>
        /// <param name="eventInfo">收到的事件实例。</param>
        public void OnEvent(EventInfo eventInfo) => OnReaction((T)eventInfo);

        /// <summary>
        /// 由派生类实现具体反应逻辑。
        /// </summary>
        /// <param name="eventInfo">已转换为目标类型的事件。</param>
        protected abstract void OnReaction(T eventInfo);

        /// <summary>
        /// 释放反应资源。
        /// </summary>
        /// <remarks>
        /// 默认行为：先停用并取消订阅，再清空 Owner 引用。
        /// </remarks>
        public virtual void Dispose()
        {
            Deactivate();
            Owner = null;
        }
    }


    /// <summary>
    /// 基于委托的通用反应实现。
    /// </summary>
    /// <typeparam name="T">监听的事件类型。</typeparam>
    /// <remarks>
    /// 适用于一次性或轻量反应定义，避免为简单逻辑单独创建派生类。
    /// </remarks>
    public class DelegateReaction<T> : UnitReaction<T> where T : EventInfo, new()
    {
        /// <summary>
        /// 事件触发时执行的回调。
        /// </summary>
        private readonly Action<T> _reactionAction;

        /// <summary>
        /// 反应唯一标识。
        /// </summary>
        public override UnitTag Name { get; }

        /// <summary>
        /// 回调优先级。
        /// </summary>
        public override int Priority { get; }

        /// <summary>
        /// 创建一个委托反应。
        /// </summary>
        /// <param name="owner">归属 Unit。</param>
        /// <param name="name">反应标识。</param>
        /// <param name="reactionAction">命中事件时执行的逻辑。</param>
        /// <param name="priority">事件优先级，数值越大越先执行。</param>
        public DelegateReaction(IUnit owner, UnitTag name, Action<T> reactionAction, int priority = 0) : base(owner)
        {
            Name = name;
            _reactionAction = reactionAction;
            Priority = priority;
        }

        /// <summary>
        /// 执行委托回调。
        /// </summary>
        /// <param name="eventInfo">事件实例。</param>
        protected override void OnReaction(T eventInfo)
        {
            _reactionAction?.Invoke(eventInfo);
        }
    }
}