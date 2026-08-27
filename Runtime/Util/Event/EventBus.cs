using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 事件总线，负责按事件类型管理和分发监听器（EventCore 内部使用，通常无需直接操作）。
    /// 支持优先级排序（Priority 越大越先执行）、过滤器拦截（OnFilter）和中断传播（IsBreak）。
    /// </summary>
    public sealed class EventBus : IDisposable
    {
        private readonly Dictionary<Type, object> _listenerMaps = new();
        private readonly HashSet<Type> _dirtyTypes = new();

        /// <summary>
        /// 订阅指定类型的事件，返回 IDisposable，调用其 Dispose 即取消订阅。
        /// 同一监听器不会重复添加。
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
                _dirtyTypes.Add(type);
            }
        }

        /// <summary>
        /// 发布事件，按优先级降序调用监听器；IsBreak 为 true 时停止后续分发。
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
        /// 清空全部监听器。
        /// </summary>
        public void Dispose()
        {
            _listenerMaps.Clear();
            _dirtyTypes.Clear();
        }
    }
}