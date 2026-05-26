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


        /// <summary>
        /// 获取事件实例，务必在使用完毕后通过 PoolCore.Return(evt) 归还实例以避免内存泄漏
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        public static TEvent GetEvent<TEvent>() where TEvent : EventData, new()
        {
            return PoolCore.Get<TEvent>();
        }


        public static void Publish<TEvent>(TEvent evt, string busName = DefaultBusName) where TEvent : EventData, new()
        {
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