using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GoveKits.Event
{
    /// <summary>
    /// 事件总线（单个管道）
    /// </summary>
    public class EventBus
    {
        // 事件类型到回调列表的映射
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new Dictionary<Type, List<Delegate>>();

        // 线程安全锁对象
        private readonly object _lockObj = new object();

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<T>(T eventData) where T : DataEvent
        {
            if (eventData == null)
            {
                Debug.LogError("[EventBus] Cannot publish null event");
                return;
            }

            Type eventType = typeof(T);
            List<Delegate> handlers;

            lock (_lockObj)
            {
                if (!_eventHandlers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
                {
                    return;
                }
                
                // 创建副本避免修改原始列表
                handlers = new List<Delegate>(handlers);
            }

            // 处理事件
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Action<T> typedHandler)
                    {
                        typedHandler(eventData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error handling event {eventType.Name}: {ex}");
                }
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public Action Subscribe<T>(Action<T> callback) where T : DataEvent
        {
            if (callback == null)
            {
                Debug.LogError("[EventBus] Cannot subscribe with null callback");
                return null;
            }

            Type eventType = typeof(T);

            lock (_lockObj)
            {
                if (!_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers = new List<Delegate>();
                    _eventHandlers[eventType] = handlers;
                }

                handlers.Add(callback);
            }
            return () => Unsubscribe(callback);
        }

        /// <summary>
        /// 订阅一次性事件，触发后自动取消订阅
        /// </summary>
        public Action SubscribeOnce<T>(Action<T> callback) where T : DataEvent
        {
            Action<T> wrappedCallback = null;
            wrappedCallback = (data) =>
            {
                try
                {
                    callback(data);
                }
                finally
                {
                    Unsubscribe(wrappedCallback);
                }
            };
            Subscribe(wrappedCallback);
            return () => Unsubscribe(wrappedCallback);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> callback) where T : DataEvent
        {
            if (callback == null) return;

            Type eventType = typeof(T);

            lock (_lockObj)
            {
                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.RemoveAll(h => h == (Delegate)callback);

                    if (handlers.Count == 0)
                    {
                        _eventHandlers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnsubscribeAll<T>() where T : DataEvent
        {
            Type eventType = typeof(T);

            lock (_lockObj)
            {
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers.Remove(eventType);
                }
            }
        }
    }
}