using System;
using System.Collections.Generic;

namespace GoveKits.Events
{
    /// <summary>
    /// 全局事件管理器 (Facade)。
    /// <para>管理多个 EventBus，并提供对 MainBus 的静态快捷访问。</para>
    /// </summary>
    public static class EventManager
    {
        private const string MainBusName = "Main";
        
        /// <summary>
        /// 默认的主事件总线。
        /// </summary>
        public static EventBus MainBus { get; } = new EventBus(MainBusName);
        
        private static readonly Dictionary<string, EventBus> _buses = new Dictionary<string, EventBus>
        {
            { MainBusName, MainBus }
        };

        #region Bus Management

        /// <summary>
        /// 尝试获取指定名称的事件总线。
        /// </summary>
        /// <param name="name">总线名称。</param>
        /// <param name="bus">输出的事件总线实例。</param>
        /// <returns>是否找到。</returns>
        public static bool TryGetBus(string name, out EventBus bus) 
            => _buses.TryGetValue(name, out bus);

        /// <summary>
        /// 创建一个新的事件总线。
        /// </summary>
        /// <param name="name">总线名称（需唯一）。</param>
        /// <returns>创建的事件总线。</returns>
        /// <exception cref="Exception">当名称重复时抛出。</exception>
        public static EventBus CreateBus(string name)
        {
            if (_buses.ContainsKey(name))
                throw new Exception($"[EventManager] Bus '{name}' already exists.");

            var bus = new EventBus(name);
            _buses[name] = bus;
            return bus;
        }

        /// <summary>
        /// 移除事件总线（主总线不可移除）。
        /// </summary>
        /// <param name="name">总线名称。</param>
        /// <returns>是否移除成功。</returns>
        public static bool RemoveBus(string name)
        {
            if (name == MainBusName) return false; // 保护主总线
            return _buses.Remove(name);
        }

        #endregion

        #region Facade API (Forward to Bus)

        // --- 订阅 ---

        /// <summary>
        /// 订阅指定总线的事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="listener">监听器实例。</param>
        /// <param name="busName">总线名称，默认主总线。</param>
        /// <returns>取消订阅操作。</returns>
        public static Action Subscribe<T>(EventListener<T> listener, string busName = MainBusName) where T : EventInfo
        {
            if (TryGetBus(busName, out var bus))
            {
                return bus.Subscribe<T>(listener);
            }
            throw new Exception($"[EventManager] Bus '{busName}' not found.");
        }

        /// <summary>
        /// 订阅指定总线的事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="action">回调处理。</param>
        /// <param name="busName">总线名称，默认主总线。</param>
        /// <param name="priority">优先级（越小越先执行）。</param>
        /// <returns>取消订阅操作。</returns>
        public static Action Subscribe<T>(Action<T> action, string busName = MainBusName, int priority = EventPriority.Normal) where T : EventInfo
        {
            if (TryGetBus(busName, out var bus))
            {
                return bus.Subscribe(action, priority);
            }
            throw new Exception($"[EventManager] Bus '{busName}' not found.");
        }

        // --- 取消订阅 ---

        /// <summary>
        /// 取消订阅。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="listener">监听器实例。</param>
        /// <param name="busName">总线名称，默认主总线。</param>
        public static void Unsubscribe<T>(EventListener<T> listener, string busName = MainBusName) where T : EventInfo
        {
            if (TryGetBus(busName, out var bus)) bus.Unsubscribe(listener);
        }

        // --- 发布 ---

        /// <summary>
        /// 在指定总线发布事件。
        /// </summary>
        /// <typeparam name="T">事件类型（需有无参构造）。</typeparam>
        /// <param name="initializer">初始化回调（设置事件字段）。</param>
        /// <param name="busName">总线名称，默认主总线。</param>
        public static void Publish<T>(Action<T> initializer = null, string busName = MainBusName) where T : EventInfo, new()
        {
            if (TryGetBus(busName, out var bus)) bus.Publish(initializer);
            else throw new Exception($"[EventManager] Bus '{busName}' not found.");
        }

        #endregion
    }
}