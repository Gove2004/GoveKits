using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core.Event
{
    #region Event Bus

    /// <summary>
    /// 单个事件总线实例。
    /// </summary>
    /// <remarks>
    /// 以“事件类型 -> 频道”组织监听器，每个频道只处理一种事件类型。
    /// </remarks>
    public class EventBus
    {
        // 使用非泛型字典集中管理不同事件类型对应的频道实例。
        private readonly Dictionary<Type, IEventChannel> _channels = new();

        /// <summary>
        /// 发布一个事件实例。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="eventInfo">事件实例。</param>
        public void Publish<T>(T eventInfo) where T : EventInfo, new()
        {
            var type = typeof(T);
            if (_channels.TryGetValue(type, out var channel))
            {
                // 每个 type 只会创建对应的 EventChannel<T>，这里的转换是受控的。
                ((EventChannel<T>)channel).Publish(eventInfo);
            }
        }

        /// <summary>
        /// 订阅指定类型事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="listener">监听器实例。</param>
        public void Subscribe<T>(IEventListener<T> listener) where T : EventInfo, new()
        {
            var type = typeof(T);
            if (!_channels.TryGetValue(type, out var channel))
            {
                channel = new EventChannel<T>();
                _channels[type] = channel;
            }
            ((EventChannel<T>)channel).Add(listener);
        }

        /// <summary>
        /// 取消订阅指定类型事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="listener">监听器实例。</param>
        public void Unsubscribe<T>(IEventListener<T> listener) where T : EventInfo, new()
        {
            if (_channels.TryGetValue(typeof(T), out var channel))
            {
                ((EventChannel<T>)channel).Remove(listener);
                if (((EventChannel<T>)channel).IsEmpty)
                {
                    // 频道没有监听器时移除，可减少字典常驻项。
                    _channels.Remove(typeof(T));
                }
            }
                
        }

    #endregion

    #region Event Channel

        // 内部频道接口
        private interface IEventChannel { }

        /// <summary>
        /// 某一具体事件类型的监听器频道。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        private class EventChannel<T> : IEventChannel where T : EventInfo, new()
        {
            private readonly List<IEventListener<T>> _listeners = new();
            private bool _isDirty;
            public bool IsEmpty => _listeners.Count == 0;

            public void Add(IEventListener<T> listener) { _listeners.Add(listener); _isDirty = true; }
            public void Remove(IEventListener<T> listener) => _listeners.Remove(listener);

            public void Publish(T eventInfo)
            {
                // 没有监听器时直接返回，避免不必要的排序和数组分配。
                if (_listeners.Count == 0) return;

                if (_isDirty)
                {
                    // 约定：Priority 越大越先执行。
                    _listeners.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                    _isDirty = false;
                }

                // 这里用快照避免回调中增删订阅导致遍历失效。
                var snapshot = _listeners.ToArray();
                foreach (var listener in snapshot)
                {
                    listener.OnEvent(eventInfo);

                    if (eventInfo.IsBreak) break;
                }
            }
        }

    #endregion
    }
}