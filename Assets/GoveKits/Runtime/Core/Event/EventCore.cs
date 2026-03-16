using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core.Pool;

namespace GoveKits.Runtime.Core.Event
{
    /// <summary>
    /// 事件系统静态入口。
    /// </summary>
    /// <remarks>
    /// 对外提供总线管理、事件发布与订阅 API。
    /// 发布流程与对象池整合，事件对象会在分发结束后统一回收。
    /// </remarks>
    public static class EventCore
    {
        // 默认总线名称，提供一个主总线供大多数事件使用，避免过度分散。
        private const string DefaultBusName = "main";

#if UNITY_EDITOR
        // 编辑器专用：事件系统变更时触发，用于刷新调试窗口。
        public static List<string> EventHistory = new List<string>();
        public static Action OnEventSystemChanged; // 用于刷新 UI
#endif

        #region Event Bus

        private static readonly Dictionary<string, EventBus> _eventBuses = new()
        {
            { DefaultBusName, new EventBus() }
        };

        /// <summary>
        /// 获取或创建指定名称的事件总线。
        /// </summary>
        /// <param name="busName">总线名称。</param>
        /// <returns>对应名称的 EventBus。</returns>
        public static EventBus GetOrCreateBus(string busName)
        {
            if (!_eventBuses.TryGetValue(busName, out var bus))
            {
                bus = new EventBus();
                _eventBuses[busName] = bus;
#if UNITY_EDITOR
                OnEventSystemChanged?.Invoke();
#endif
            }
            return bus;
        }

        /// <summary>
        /// 尝试获取指定总线，不存在时不创建。
        /// </summary>
        /// <param name="busName">总线名称。</param>
        /// <param name="bus">找到时返回对应总线。</param>
        /// <returns>是否找到总线。</returns>
        public static bool TryGetBus(string busName, out EventBus bus)
        {
            return _eventBuses.TryGetValue(busName, out bus);
        }

        /// <summary>
        /// 销毁指定事件总线。
        /// </summary>
        /// <param name="busName">总线名称。</param>
        public static void DestroyBus(string busName)
        {
            if (busName == DefaultBusName)
            {
                GoveKitsCore.Log(nameof(EventCore), $"禁止销毁默认事件总线", logType: GoveKitsCore.LogType.Error);
                return;
            }

            if (_eventBuses.Remove(busName))
            {
#if UNITY_EDITOR
                OnEventSystemChanged?.Invoke();
#endif
            }
        }

        #endregion

        #region Event Info

        // 不推荐直接传入 EventInfo 对象，因为外部可能会复用这个对象，导致事件系统内的状态混乱。
        // public static void Publish<T>(T eventInfo, string busName = DefaultBusName) where T : EventInfo, new()
        // {
        //     EventBus bus = GetOrCreateBus(busName);
        //     bus.Publish(eventInfo);
        // }

        /// <summary>
        /// 发布一个事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="eventIniter">事件初始化回调，用于填充事件内容。</param>
        /// <param name="busName">目标总线名称，默认主总线。</param>
        /// <remarks>
        /// 流程：从池取对象 -> 初始化 -> 分发 -> finally 中重置并归还。
        /// 即使分发抛出异常，也会执行回收逻辑。
        /// </remarks>
        public static void Publish<T>(Action<T> eventIniter, string busName = DefaultBusName) where T : EventInfo, new()
        {
            if (eventIniter == null)
            {
                throw new ArgumentNullException(nameof(eventIniter));
            }

            // 从池中获取事件对象，减少临时分配和 GC 压力。
            var eventInfo = PoolCore.Get<T>();
            eventIniter(eventInfo);
            EventBus bus = GetOrCreateBus(busName);
            try
            {
#if UNITY_EDITOR
            // 记录历史
            string log = $"[{DateTime.Now:HH:mm:ss}] Bus: {busName} | Event: {typeof(T).Name}";
            EventHistory.Insert(0, log);
            if (EventHistory.Count > 50) EventHistory.RemoveAt(50);
            OnEventSystemChanged?.Invoke();
#endif
                bus.Publish(eventInfo);
            }
            catch (Exception ex)
            {
                throw new Exception($"[EventCore] Exception occurred while publishing event of type {typeof(T).Name} on bus '{busName}'.", ex);
            }
            finally
            {
                // 回收前重置中断标记，防止复用后影响下一次分发。
                eventInfo.IsBreak = false;
                PoolCore.Return(eventInfo);
            }
        }

        /// <summary>
        /// 订阅指定事件类型。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="listener">监听器实例。</param>
        /// <param name="busName">目标总线名称。</param>
        /// <returns>可释放的反订阅句柄。</returns>
        public static DisposeAction Subscribe<T>(IEventListener<T> listener, string busName = DefaultBusName) where T : EventInfo, new()
        {
            EventBus bus = GetOrCreateBus(busName);
            bus.Subscribe<T>(listener);
#if UNITY_EDITOR
            OnEventSystemChanged?.Invoke();
#endif
            return new DisposeAction(() =>
            {
                bus.Unsubscribe<T>(listener);
#if UNITY_EDITOR
                OnEventSystemChanged?.Invoke();
#endif
            });
        }

        /// <summary>
        /// 通过委托订阅指定事件类型。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="callback">事件回调。</param>
        /// <param name="priority">监听器优先级，数值越大越先执行。</param>
        /// <param name="busName">目标总线名称。</param>
        /// <returns>可释放的反订阅句柄。</returns>
        public static DisposeAction Subscribe<T>(Action<T> callback, int priority = 0, string busName = DefaultBusName) where T : EventInfo, new()
        {
            var listener = new ActionEventListener<T>(callback, priority);
            return Subscribe<T>(listener, busName);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 获取当前已存在的总线名快照（用于编辑器调试显示）。
        /// </summary>
        public static List<string> GetDebugBusNames()
        {
            var busNames = new List<string>(_eventBuses.Keys);
            busNames.Sort(StringComparer.Ordinal);
            return busNames;
        }
#endif

        #endregion
    }
}