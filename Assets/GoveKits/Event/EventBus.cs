using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GoveKits.Event
{


    /// <summary>
    /// 事件总线
    /// </summary>
    public class EventBus
    {
        // 事件类型到回调列表的映射
        private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();

        // 线程安全锁对象
        private static readonly object lockObj = new object();

        /// <summary>
        /// 发布事件
        /// </summary>
        public static void Publish<T>(T eventData) where T : DataEvent
        {
            if (eventData == null)
            {
                Debug.LogError("[EventBus] Cannot publish null event");
                return;
            }

            Type eventType = typeof(T);
            List<Delegate> handlers;  // 使用固定大小的环形缓冲区

            lock (lockObj)
            {
                if (!eventHandlers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
                {
                    Debug.Log($"[EventBus] No subscribers for event type {eventType.Name}");
                    return;
                }
            }

            // 直接遍历原始handlers列表
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
        public static void Subscribe<T>(Action<T> callback) where T : DataEvent
        {
            if (callback == null)
            {
                Debug.LogError("[EventBus] Cannot subscribe with null callback");
                return;
            }

            Type eventType = typeof(T);

            lock (lockObj)
            {
                if (!eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers = new List<Delegate>();
                    eventHandlers[eventType] = handlers;
                }

                handlers.Add(callback);
            }
        }

        /// <summary>
        /// 订阅一次性事件，触发后自动取消订阅
        /// </summary>
        public static void SubscribeOnce<T>(Action<T> callback) where T : DataEvent
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
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : DataEvent
        {
            if (callback == null) return;

            Type eventType = typeof(T);

            lock (lockObj)
            {
                if (eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    // 直接比较委托引用
                    handlers.RemoveAll(h => h == (Delegate)callback);

                    // 清理空列表
                    if (handlers.Count == 0)
                    {
                        eventHandlers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// 取消所有订阅（通常用于场景切换或对象销毁时）
        /// </summary>
        public static void UnsubscribeAll<T>() where T : DataEvent
        {
            Type eventType = typeof(T);

            lock (lockObj)
            {
                if (eventHandlers.ContainsKey(eventType))
                {
                    eventHandlers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// 获取事件类型的订阅者数量
        /// </summary>
        public static int GetSubscriberCount<T>() where T : DataEvent
        {
            Type eventType = typeof(T);

            lock (lockObj)
            {
                return eventHandlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
            }
        }
    }

}


