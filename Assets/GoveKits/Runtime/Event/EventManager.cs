


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GoveKits.Event
{
    /// <summary>
    /// 事件管理器（单例）- 支持多管道事件系统
    /// </summary>
    public class EventManager : MonoSingleton<EventManager>
    {
        #region Channel Management
        // 事件管道字典
        private readonly Dictionary<EventChannel, EventBus> _channels = new Dictionary<EventChannel, EventBus>();
        
        // 通道启用状态
        private readonly Dictionary<EventChannel, bool> _channelEnabled = new Dictionary<EventChannel, bool>();

        /// <summary>
        /// 初始化事件管理器
        /// </summary>
        public void Initialize()
        {
            // 初始化所有管道
            foreach (EventChannel channel in Enum.GetValues(typeof(EventChannel)))
            {
                _channels[channel] = new EventBus();
                _channelEnabled[channel] = true;
            }
            
            Debug.Log($"[EventManager] Initialized {_channels.Count} event channels.");
        }

        /// <summary>
        /// 启用/禁用特定管道的事件处理
        /// </summary>
        public void SetChannelEnabled(EventChannel channel, bool enabled)
        {
            if (_channelEnabled.ContainsKey(channel))
            {
                _channelEnabled[channel] = enabled;
                Debug.Log($"[EventManager] Channel {channel} {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// 检查管道是否启用
        /// </summary>
        public bool IsChannelEnabled(EventChannel channel)
        {
            return _channelEnabled.ContainsKey(channel) && _channelEnabled[channel];
        }

        /// <summary>
        /// 获取特定管道的事件总线
        /// </summary>
        public EventBus GetChannel(EventChannel channel)
        {
            return _channels.TryGetValue(channel, out var bus) ? bus : null;
        }
        #endregion

        #region Public API - Channel Specific
        /// <summary>
        /// 发布事件到指定管道
        /// </summary>
        public void Publish<T>(T eventData, EventChannel channel = EventChannel.Global) where T : DataEvent
        {
            if (!IsChannelEnabled(channel))
            {
                Debug.Log($"[EventManager] Channel {channel} is disabled, event dropped.");
                return;
            }

            if (_channels.TryGetValue(channel, out var bus))
            {
                bus.Publish(eventData);
            }
        }

        /// <summary>
        /// 发布事件到多个管道
        /// </summary>
        public void PublishToChannels<T>(T eventData, params EventChannel[] channels) where T : DataEvent
        {
            foreach (var channel in channels)
            {
                Publish(eventData, channel);
            }
        }

        /// <summary>
        /// 订阅指定管道的事件
        /// </summary>
        public Action Subscribe<T>(Action<T> callback, EventChannel channel = EventChannel.Global) where T : DataEvent
        {
            if (_channels.TryGetValue(channel, out var bus))
            {
                bus.Subscribe(callback);
            }
            return () => Unsubscribe(callback, channel);
        }

        /// <summary>
        /// 订阅多个管道的事件
        /// </summary>
        public Action SubscribeToChannels<T>(Action<T> callback, params EventChannel[] channels) where T : DataEvent
        {
            foreach (var channel in channels)
            {
                Subscribe(callback, channel);
            }
            return () => UnsubscribeFromChannels(callback, channels);
        }

        /// <summary>
        /// 一次性订阅指定管道的事件
        /// </summary>
        public Action SubscribeOnce<T>(Action<T> callback, EventChannel channel = EventChannel.Global) where T : DataEvent
        {
            if (_channels.TryGetValue(channel, out var bus))
            {
                bus.SubscribeOnce(callback);
            }
            return () => Unsubscribe(callback, channel);
        }

        /// <summary>
        /// 取消订阅指定管道的事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> callback, EventChannel channel = EventChannel.Global) where T : DataEvent
        {
            if (_channels.TryGetValue(channel, out var bus))
            {
                bus.Unsubscribe(callback);
            }
        }

        /// <summary>
        /// 取消订阅多个管道的事件
        /// </summary>
        public void UnsubscribeFromChannels<T>(Action<T> callback, params EventChannel[] channels) where T : DataEvent
        {
            foreach (var channel in channels)
            {
                Unsubscribe(callback, channel);
            }
        }
        #endregion

        #region Bulk Operations
        /// <summary>
        /// 清空指定管道的所有订阅
        /// </summary>
        public void ClearChannel(EventChannel channel)
        {
            if (_channels.TryGetValue(channel, out var bus))
            {
                // 这里需要为EventBus添加Clear方法
                // bus.Clear();
                Debug.LogWarning($"[EventManager] Clear method not implemented for channel {channel}");
            }
        }

        /// <summary>
        /// 清空所有管道的订阅
        /// </summary>
        public void ClearAllChannels()
        {
            foreach (var channel in _channels.Keys.ToList())
            {
                ClearChannel(channel);
            }
        }
        #endregion

        // #region Debug & Monitoring
        // /// <summary>
        // /// 启用调试日志
        // /// </summary>
        // public bool EnableDebugLogs { get; set; } = false;

        // /// <summary>
        // /// 事件处理监控
        // /// </summary>
        // public event Action<string, EventChannel, long> OnEventProcessed; // 事件类型, 管道, 处理时间(ms)

        // private void LogEvent<T>(EventChannel channel, string operation)
        // {
        //     if (EnableDebugLogs)
        //     {
        //         Debug.Log($"[EventManager] {operation} event {typeof(T).Name} on channel {channel}");
        //     }
        // }
        // #endregion
    }
}