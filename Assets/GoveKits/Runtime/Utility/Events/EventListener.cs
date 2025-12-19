using System;

namespace GoveKits.Events
{
    /// <summary>
    /// 事件监听器非泛型基类，用于在集合中存储。
    /// </summary>
    public abstract class EventListener
    {
        /// <summary>
        /// 监听优先级。数值越小越先执行。
        /// </summary>
        public virtual int Priority => EventPriority.Normal;
        
        /// <summary>
        /// 接收事件的内部入口。
        /// </summary>
        internal abstract void OnReceive(EventInfo evt);
    }

    /// <summary>
    /// 泛型监听器基类。自定义复杂监听器（如单独的类）请继承此项。
    /// </summary>
    public abstract class EventListener<T> : EventListener where T : EventInfo
    {
        internal override void OnReceive(EventInfo evt)
        {
            // 这里的强转是安全的，因为 EventChannel 保证了类型一致性
            if (evt is T tEvt)
            {
                OnHandle(tEvt);
            }
        }

        /// <summary>
        /// 处理具体类型的事件。
        /// </summary>
        /// <param name="evt">事件对象。</param>
        protected abstract void OnHandle(T evt);
    }

    /// <summary>
    /// 委托监听器。用于 Lambda 表达式或 Action 快速订阅。
    /// </summary>
    public class DelegateListener<T> : EventListener<T> where T : EventInfo
    {
        private readonly Action<T> _action;
        private readonly int _priority;

        public override int Priority => _priority;

        /// <summary>
        /// 创建一个基于委托的监听器。
        /// </summary>
        /// <param name="action">回调委托。</param>
        /// <param name="priority">优先级（越小越先执行）。</param>
        public DelegateListener(Action<T> action, int priority = EventPriority.Normal)
        {
            _action = action;
            _priority = priority;
        }

        /// <inheritdoc />
        protected override void OnHandle(T evt)
        {
            _action?.Invoke(evt);
        }
    }
}