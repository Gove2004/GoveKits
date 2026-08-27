using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 事件系统统一入口。
    /// 管理事件对象的池化复用和监听器分发。
    ///
    /// 典型用法：
    ///   var evt = EventCore.GetEvent<MyEvent>();
    ///   evt.SomeData = value;
    ///   EventCore.Publish(evt);
    /// （GetEvent 自动从 PoolCore 获取，Publish 自动归还，调用方无需手动池化）
    /// </summary>
    public static class EventCore
    {
        private static EventBus bus = new EventBus();

        /// <summary>
        /// 从池中获取指定类型的事件对象。
        /// 事件类型必须实现 IPoolable 并提供默认构造函数。
        /// 获取后填充事件数据，再调用 Publish 发布。
        /// </summary>
        public static TEvent GetEvent<TEvent>() where TEvent : EventData, new()
        {
            return PoolCore.Get<TEvent>();
        }

        /// <summary>
        /// 发布事件对象，分发到所有匹配的监听器后自动归还到池中。
        /// 无论发布过程是否抛出异常，事件对象都会被正确归还。
        /// </summary>
        public static void Publish<TEvent>(TEvent evt) where TEvent : EventData, new()
        {
            if (bus == null) return;

            try
            {
                bus.Publish(evt);
            }
            finally
            {
                PoolCore.Return(evt);
            }
        }

        /// <summary>
        /// 订阅指定类型的事件，返回 IDisposable 用于取消订阅。
        /// </summary>
        public static IDisposable Subscribe<TEvent>(IEventListener<TEvent> listener) where TEvent : EventData
        {
            return bus?.Subscribe(listener);
        }

        /// <summary>
        /// 关闭事件总线，清空所有监听器和待处理事件。
        /// 通常在场景切换或应用退出时调用。
        /// </summary>
        public static void Close()
        {
            bus?.Dispose();
            bus = null;
        }
    }
}
