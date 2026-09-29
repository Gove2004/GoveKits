using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 事件总线，负责按事件类型管理和分发监听器（EventCore 内部使用，通常无需直接操作）。
    /// 支持优先级排序（Priority 越大越先执行）、过滤器拦截（OnFilter）和中断传播（IsBreak）。
    /// 监听器回调异常会被逐个隔离，不影响同轮其他监听器。
    /// </summary>
    public sealed class EventBus : IDisposable
    {
        private readonly Dictionary<Type, object> _listenerMaps = new();
        // 已排序的监听器快照缓存：仅在订阅/退订（dirty）时重建，发布路径零分配
        private readonly Dictionary<Type, object> _snapshots = new();
        private readonly HashSet<Type> _dirtyTypes = new();

        /// <summary>
        /// 订阅指定类型的事件，返回 IDisposable，调用其 Dispose 即取消订阅。
        /// 同一监听器不会重复添加。
        /// 注意：Priority 应在订阅前设置，运行期修改不会触发重排。
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

                // 空列表连同快照一起回收，避免类型条目无界增长
                if (listeners.Count == 0)
                {
                    _listenerMaps.Remove(type);
                    _snapshots.Remove(type);
                    _dirtyTypes.Remove(type);
                }
            }
        }

        /// <summary>
        /// 发布事件，按优先级降序调用监听器；IsBreak 为 true 时停止后续分发。
        /// 单个监听器抛出的异常会被捕获并记录，不影响其余监听器。
        /// </summary>
        internal void Publish<TEvent>(TEvent eventData) where TEvent : EventData
        {
            var type = typeof(TEvent);
            if (!_listenerMaps.TryGetValue(type, out var listObj)) return;

            var listeners = (List<IEventListener<TEvent>>)listObj;
            if (listeners.Count == 0) return;

            IEventListener<TEvent>[] snapshot;
            if (_dirtyTypes.Remove(type))
            {
                listeners.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                snapshot = listeners.ToArray();
                _snapshots[type] = snapshot;
            }
            else
            {
                snapshot = (IEventListener<TEvent>[])_snapshots[type];
            }

            foreach (var listener in snapshot)
            {
                // 与 C# 事件语义一致：分发期间退订的监听器，本轮（已在快照中）仍会收到，下轮生效
                try
                {
                    if (listener.OnFilter(eventData))
                    {
                        listener.OnEvent(eventData);
                        if (eventData.IsBreak) break;
                    }
                }
                catch (Exception e)
                {
                    LogCore.Error(nameof(EventBus), $"监听器 {listener.GetType().Name} 处理 {type.Name} 时异常: {e}");
                }
            }
        }

        /// <summary>
        /// 清空全部监听器。
        /// </summary>
        public void Dispose()
        {
            _listenerMaps.Clear();
            _snapshots.Clear();
            _dirtyTypes.Clear();
        }
    }
}
