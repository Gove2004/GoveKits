using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Core
{
    public class EventBus : IDisposable
    {
        // 核心技巧：Value 使用 object，实际存储的是 List<IEventListener<T>>
        private readonly Dictionary<Type, object> _listenerMaps = new();
        
        // 记录哪些类型的列表需要重新排序（因为加入了新监听器）
        private readonly HashSet<Type> _dirtyTypes = new();

        /// <summary>
        /// 订阅事件
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
                _dirtyTypes.Add(type); // 标记需要重新排序
            }

            // 返回取消订阅的凭证
            return new DisposeAction(() => Unsubscribe(listener));
        }

        /// <summary>
        /// 取消订阅 (供内部或凭证调用)
        /// </summary>
        private void Unsubscribe<TEvent>(IEventListener<TEvent> listener) where TEvent : EventData
        {
            var type = typeof(TEvent);
            if (_listenerMaps.TryGetValue(type, out var listObj))
            {
                var listeners = (List<IEventListener<TEvent>>)listObj;
                listeners.Remove(listener);
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        internal void Publish<TEvent>(TEvent eventData) where TEvent : EventData
        {
            var type = typeof(TEvent);
            if (!_listenerMaps.TryGetValue(type, out var listObj)) return;

            var listeners = (List<IEventListener<TEvent>>)listObj;
            if (listeners.Count == 0) return;

            // 如果有新加入的，触发排序
            if (_dirtyTypes.Contains(type))
            {
                listeners.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // 降序：数字越大越先执行
                _dirtyTypes.Remove(type);
            }

            // 使用快照遍历，防止在 OnEvent 中动态添加/移除监听器导致遍历报错
            var snapshot = listeners.ToArray(); 
            
            foreach (var listener in snapshot)
            {
                if (listener.OnFilter(eventData))
                {
                    listener.OnEvent(eventData);
                    
                    // 中断机制：比如高优先级的 UI 吞噬了点击事件
                    if (eventData.IsBreak)
                    {
                        break; 
                    }
                }
            }
        }

        public void Dispose()
        {
            _listenerMaps.Clear();
            _dirtyTypes.Clear();
        }
    }
}