using System;
using System.Collections.Generic;

namespace GoveKits.Events
{
    /// <summary>
    /// 事件总线。代表一个独立的事件作用域（如：主世界、副本A、UI系统）。
    /// </summary>
    public class EventBus
    {
        public string Name { get; private set; }
        
        // Key: 事件类型 Type, Value: 该类型的频道 Channel
        private readonly Dictionary<Type, EventChannel> _channels = new Dictionary<Type, EventChannel>();

        public EventBus(string name) { Name = name; }

        #region 订阅 (Subscribe)

        /// <summary>
        /// 订阅事件。
        /// </summary>
        /// <param name="listener">监听器实例。</param>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <returns>返回一个取消订阅的操作。</returns>
        public Action Subscribe<T>(EventListener<T> listener) where T : EventInfo
        {
            var type = typeof(T);
            if (!_channels.TryGetValue(type, out var channel))
            {
                channel = new EventChannel();
                _channels[type] = channel;
            }
            channel.Add(listener);
            return () => Unsubscribe(listener);
        }

        /// <summary>
        /// 使用 Action 订阅事件。
        /// </summary>
        /// <param name="action">回调处理。</param>
        /// <param name="priority">优先级（越小越先执行）。</param>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <returns>返回一个取消订阅的操作。</returns>
        public Action Subscribe<T>(Action<T> action, int priority = EventPriority.Normal) where T : EventInfo
        {
            var listener = new DelegateListener<T>(action, priority);
            Subscribe(listener);
            
            // 返回一个闭包，捕获 listener 和 this，用于取消订阅
            return () => Unsubscribe(listener);
        }

        #endregion

        #region 取消订阅 (Unsubscribe)

        /// <summary>
        /// 取消订阅某类型事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="listener">监听器实例。</param>
        public void Unsubscribe<T>(EventListener<T> listener) where T : EventInfo
        {
            var type = typeof(T);
            if (_channels.TryGetValue(type, out var channel))
            {
                channel.Remove(listener);
            }
        }

        #endregion

        #region 发布 (Publish)

        // 内部发布逻辑，直接分发给 Channel
        internal void PublishInternal(EventInfo evt)
        {
            if (_channels.TryGetValue(evt.GetType(), out var channel))
            {
                channel.Publish(evt);
            }
        }

        /// <summary>
        /// 发布事件（对象池模式）。
        /// </summary>
        /// <param name="initializer">初始化回调，用于设置事件参数。</param>
        /// <typeparam name="T">事件类型（需无参构造）。</typeparam>
        public void Publish<T>(Action<T> initializer = null) where T : EventInfo, new()
        {
            var evt = Pools.Pool.Get<T>();
            try
            {
                initializer?.Invoke(evt);
                PublishInternal(evt);
            }
            finally
            {
                // 无论是否发生异常，都必须回收事件对象
                evt.IsStopped = false; // 框架级的重置
                Pools.Pool.Recycle(evt);
            }
        }

        #endregion
    }
}
