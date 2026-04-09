using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    public class EventCore : ICore
    {
        public const string DefaultBusName = "Global";
        private readonly Dictionary<string, EventBus> _buses = new();

        /// <summary>
        /// 获取或创建指定通道的总线
        /// </summary>
        public EventBus GetOrCreateBus(string channel = DefaultBusName)
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
        public void ClearBus(string channel)
        {
            if (_buses.TryGetValue(channel, out var bus))
            {
                bus.Dispose();
                _buses.Remove(channel);
            }
        }

        public void Publish<TEvent>(Action<TEvent> initer, string busName = DefaultBusName) where TEvent : EventData, new()
        {
            TEvent evt = CoreLocator.Pool.Get<TEvent>();
            initer?.Invoke(evt);
            try
            {
                EventBus bus = GetOrCreateBus(busName);
                bus.Publish(evt);
            }
            finally
            {
                CoreLocator.Pool.Return(evt);
            }
        }

        /// <summary>
        /// 订阅事件，务必保存返回的 IDisposable 用于取消订阅
        /// </summary>
        public IDisposable Subscribe<TEvent>(IEventListener<TEvent> listener, string busName = DefaultBusName) where TEvent : EventData
        {
            return GetOrCreateBus(busName).Subscribe(listener);
        }

        public void OnShutdown()
        {
            foreach (var bus in _buses.Values)
            {
                bus.Dispose();
            }
            _buses.Clear();
        }
    }
}