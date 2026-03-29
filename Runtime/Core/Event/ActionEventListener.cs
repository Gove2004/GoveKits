using System;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// Action 包装的事件监听器
    /// </summary>
    /// <remarks>
    /// 将 Action 委托包装为 IEventListener 接口
    /// 适用于简单场景，无需创建独立监听器类
    /// 支持优先级设置
    /// </remarks>
    /// <typeparam name="T">监听的事件类型</typeparam>
    public class ActionEventListener<T> : IEventListener<T> where T : EventData
    {
        /// <summary>
        /// 事件回调委托
        /// </summary>
        private readonly Action<T> _action;
        
        /// <summary>
        /// 监听器优先级（只读自动属性）
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="action">事件回调方法</param>
        /// <param name="priority">优先级，默认 0</param>
        public ActionEventListener(Action<T> action, int priority = 0)
        {
            _action = action;
            Priority = priority;
        }
        
        /// <summary>
        /// 事件过滤器
        /// </summary>
        /// <remarks>
        /// 默认实现始终返回 true，接收所有事件
        /// 子类可重写此方法实现自定义过滤
        /// </remarks>
        public bool OnFilter(T eventData) => true;
        
        /// <summary>
        /// 事件处理方法
        /// </summary>
        /// <param name="eventInfo">事件数据</param>
        /// <remarks>
        /// 调用内部存储的 Action 委托
        /// 使用 ?. 操作符避免空引用异常
        /// </remarks>
        public void OnEvent(T eventInfo) => _action?.Invoke(eventInfo);
    }
}