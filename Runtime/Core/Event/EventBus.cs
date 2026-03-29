using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件总线接口（非泛型）
    /// </summary>
    /// <remarks>
    /// 标记接口，用于 Dictionary 统一存储不同类型的 EventBus
    /// 无成员定义，仅作类型标识使用
    /// </remarks>
    public interface IEventBus
    {
    }

    /// <summary>
    /// 泛型事件总线实现类
    /// </summary>
    /// <remarks>
    /// 处理单一事件类型的所有监听器管理
    /// 支持优先级排序、事件过滤、事件中断
    /// 使用快照遍历避免订阅/取消订阅时的集合修改异常
    /// </remarks>
    /// <typeparam name="T">事件类型，必须继承 EventData 且有无参构造函数</typeparam>
    public class EventBus<T> : IEventBus where T : EventData, new()
    {
        /// <summary>
        /// 监听器列表
        /// </summary>
        /// <remarks>
        /// 存储所有订阅该事件类型的监听器
        /// 使用 List 而非 Dictionary，支持重复订阅
        /// </remarks>
        private readonly List<IEventListener<T>> _listeners = new();
        
        /// <summary>
        /// 脏标记
        /// </summary>
        /// <remarks>
        /// 标记监听器列表是否发生变化
        /// 变化后下次发布前需要重新排序
        /// 避免每次发布都排序，提升性能
        /// </remarks>
        private bool _isDirty = true;
        
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="eventInfo">事件数据实例</param>
        /// <remarks>
        /// 遍历所有监听器并调用 OnEvent
        /// 支持优先级排序和事件中断
        /// 使用快照遍历避免集合修改异常
        /// </remarks>
        public void Publish(T eventInfo)
        {
            // 无监听器时直接返回
            if (_listeners.Count == 0) return;

            // 列表有变化时重新排序
            if (_isDirty)
            {
                // 按优先级降序排序（高优先级先执行）
                _listeners.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _isDirty = false;
            }

            // 创建快照，避免遍历时修改集合
            IEventListener<T>[] snapshot = _listeners.ToArray();
            foreach (var listener in snapshot)
            {
                // 通过过滤器检查
                if (listener.OnFilter(eventInfo))
                {
                    // 调用事件处理
                    listener.OnEvent(eventInfo);
                    
                    // 检查是否需要中断后续监听器
                    if (eventInfo.IsBreak)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="listener">监听器实例</param>
        /// <returns>取消订阅句柄，调用 Dispose 或 using 自动取消</returns>
        /// <remarks>
        /// 将监听器添加到列表
        /// 标记为脏，下次发布前重新排序
        /// 返回 DisposeAction 用于取消订阅
        /// </remarks>
        public DisposeAction Subscribe(IEventListener<T> listener)
        {
            _listeners.Add(listener);
            _isDirty = true;
            // 返回取消订阅句柄
            return new DisposeAction(() => _listeners.Remove(listener));
        }
    }
}