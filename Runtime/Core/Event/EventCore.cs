using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件系统静态入口
    /// </summary>
    /// <remarks>
    /// 对外提供总线管理、事件发布与订阅 API
    /// 发布流程与对象池整合，事件对象会在分发结束后统一回收
    /// 采用静态类设计，全局可访问，无需实例化
    /// </remarks>
    public static class EventCore
    {
        /// <summary>
        /// 事件总线字典
        /// </summary>
        /// <remarks>
        /// 存储所有事件类型对应的总线实例
        /// 键：事件类型 Type，值：IEventBus 接口
        /// 懒加载创建，按需分配
        /// </remarks>
        private static readonly Dictionary<Type, IEventBus> _bus = new();

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="initer">事件初始化回调</param>
        /// <remarks>
        /// 从对象池获取事件实例
        /// 调用初始化回调设置事件数据
        /// 分发结束后自动回收到对象池
        /// 包含异常捕获，防止事件处理异常影响系统
        /// </remarks>
        public static void Publish<T>(Action<T> initer) where T : EventData, new()
        {
            if (initer == null) throw new ArgumentNullException(nameof(initer));

            // 从对象池获取事件实例（减少 GC）
            var eventInfo = PoolCore.Get<T>();
            
            // 调用初始化回调
            initer(eventInfo);
            
            try
            {
                // 查找并调用对应总线
                var type = typeof(T);
                if (_bus.TryGetValue(type, out var bus))
                {
                    ((EventBus<T>)bus).Publish(eventInfo);
                }
            }
            catch (Exception ex)
            {
                // 记录异常日志
                LogCore.Error(nameof(EventCore), $"事件发布异常：{ex.Message}");
            }
            finally
            {
                // 回收前重置中断标记，防止复用后影响下一次分发
                eventInfo.IsBreak = false;
                // 归还到对象池
                PoolCore.Return(eventInfo);
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="listener">监听器实例</param>
        /// <returns>取消订阅句柄</returns>
        /// <remarks>
        /// 获取或创建对应事件类型的总线
        /// 返回取消订阅句柄，支持 using 自动管理
        /// </remarks>
        public static DisposeAction Subscribe<T>(IEventListener<T> listener) where T : EventData, new()
        {
            EventBus<T> bus = GetOrCreateBus<T>();
            return bus.Subscribe(listener);
        }

        /// <summary>
        /// 获取或创建事件总线
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>事件总线实例</returns>
        /// <remarks>
        /// 懒加载创建总线实例
        /// 同一事件类型只创建一次，后续复用
        /// 类型转换后返回
        /// </remarks>
        private static EventBus<T> GetOrCreateBus<T>() where T : EventData, new()
        {
            var type = typeof(T);
            if (!_bus.TryGetValue(type, out var bus))
            {
                bus = new EventBus<T>();
                _bus[type] = bus;
            }
            return (EventBus<T>)bus;
        }
    }
}