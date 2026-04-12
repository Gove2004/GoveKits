using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    public static class EventCore
    {
        public const string DefaultBusName = "Global";
        private static readonly Dictionary<string, EventBus> _buses = new();

        /// <summary>
        /// 获取或创建指定通道的总线
        /// </summary>
        public static EventBus GetOrCreateBus(string channel = DefaultBusName)
        {
            if (!_buses.TryGetValue(channel, out var bus))
            {
                bus = new EventBus();
                _buses.Add(channel, bus);
            }
            return bus;
        }

        /// <summary>
        /// 一键清空某个通道
        /// </summary>
        public static void ClearBus(string channel)
        {
            if (_buses.TryGetValue(channel, out var bus))
            {
                bus.Dispose();
                _buses.Remove(channel);
            }
        }

        public static void Publish<TEvent>(Action<TEvent> initer, string busName = DefaultBusName) where TEvent : EventData, new()
        {
            TEvent evt = PoolCore.Get<TEvent>();
            initer?.Invoke(evt);
            try
            {
                EventBus bus = GetOrCreateBus(busName);
                bus.Publish(evt);
            }
            finally
            {
                PoolCore.Return(evt);
            }
        }

        /// <summary>
        /// 订阅事件，务必保存返回的 IDisposable 用于取消订阅
        /// </summary>
        public static IDisposable Subscribe<TEvent>(IEventListener<TEvent> listener, string busName = DefaultBusName) where TEvent : EventData
        {
            return GetOrCreateBus(busName).Subscribe(listener);
        }

        public static void Clear()
        {
            foreach (var bus in _buses.Values)
            {
                bus.Dispose();
            }
            _buses.Clear();
        }
    }
}