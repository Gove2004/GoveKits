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

        public static bool TryGetBus(string name, out EventBus bus) 
            => _buses.TryGetValue(name, out bus);

        public static EventBus CreateBus(string name)
        {
            if (_buses.ContainsKey(name))
                throw new Exception($"[EventManager] Bus '{name}' already exists.");

            var bus = new EventBus(name);
            _buses[name] = bus;
            return bus;
        }

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
        public static Action Subscribe<T>(Action<T> action, string busName = MainBusName, int priority = EventPriority.Normal) where T : EventInfo
        {
            if (TryGetBus(busName, out var bus))
            {
                return bus.Subscribe(action, priority);
            }
            throw new Exception($"[EventManager] Bus '{busName}' not found.");
        }

        // --- 取消订阅 ---

        public static void Unsubscribe<T>(EventListener<T> listener, string busName = MainBusName) where T : EventInfo
        {
            if (TryGetBus(busName, out var bus)) bus.Unsubscribe(listener);
        }

        // --- 发布 ---

        /// <summary>
        /// 在指定总线发布事件。
        /// </summary>
        public static void Publish<T>(Action<T> initializer = null, string busName = MainBusName) where T : EventInfo, new()
        {
            if (TryGetBus(busName, out var bus)) bus.Publish(initializer);
            else throw new Exception($"[EventManager] Bus '{busName}' not found.");
        }

        #endregion
    }
}