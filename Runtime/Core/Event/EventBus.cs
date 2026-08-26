using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件总线，负责按事件类型管理和分发监听器。
    /// 支持优先级排序、过滤器拦截和中断传播（IsBreak）。
    /// 内部通过 object-dictionary 桥接泛型与非泛型注册表。
    /// </summary>
    public sealed class EventBus : IDisposable
    {
        // Value 使用 object，实际存储的是 List<IEventListener<T>>
        private readonly Dictionary<Type, object> _listenerMaps = new();
        private readonly HashSet<Type> _dirtyTypes = new();

        /// <summary>
        /// 订阅指定类型的事件，返回 IDisposable 用于取消订阅。
        /// 监听器不会重复添加，且会在下次发布前按优先级重新排序。
        /// </summary>
        internal IDisposable Subscribe<TEvent>(IEventListener<TEvent> listener) where TEvent : EventData
        {
            var type = typeof(TEvent);
            if (!_listenerMaps.TryGetValue(type, out var listObj))
            {
                listObj = new List<IEventListener<TEvent>>();
                _listenerMaps[type] = listObj;
            }

            var listeners = (List<IEventListener<TEvent>>)listObj;
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
                _dirtyTypes.Add(type);
            }

            return new DisposeAction(() => Unsubscribe(listener));
        }

        private void Unsubscribe<TEvent>(IEventListener<TEvent> listener) where TEvent : EventData
        {
            var type = typeof(TEvent);
            if (_listenerMaps.TryGetValue(type, out var listObj))
            {
                var listeners = (List<IEventListener<TEvent>>)listObj;
                listeners.Remove(listener);
                _dirtyTypes.Add(type); // 数量变化后需要重新排序
            }
        }

        /// <summary>
        /// 发布事件，按优先级降序依次调用监听器。
        /// 每个监听器的 OnFilter 决定其是否接收该事件；
        /// 事件数据的 IsBreak 为 true 时停止后续分发。
        /// </summary>
        internal void Publish<TEvent>(TEvent eventData) where TEvent : EventData
        {
            var type = typeof(TEvent);
            if (!_listenerMaps.TryGetValue(type, out var listObj)) return;

            var listeners = (List<IEventListener<TEvent>>)listObj;
            if (listeners.Count == 0) return;

            if (_dirtyTypes.Contains(type))
            {
                listeners.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _dirtyTypes.Remove(type);
            }

            var snapshot = listeners.ToArray();

            foreach (var listener in snapshot)
            {
                if (listener.OnFilter(eventData))
                {
                    listener.OnEvent(eventData);
                    if (eventData.IsBreak) break;
                }
            }
        }

        /// <summary>
        /// 释放总线占用的所有资源，清除全部监听器。
        /// </summary>
        public void Dispose()
        {
            _listenerMaps.Clear();
            _dirtyTypes.Clear();
        }
    }
}