namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件监听器接口
    /// </summary>
    /// <remarks>
    /// 定义事件监听器的标准规范
    /// 所有监听器必须实现此接口才能接收事件
    /// 支持优先级排序和事件过滤功能
    /// </remarks>
    /// <typeparam name="T">监听的事件类型，必须继承 EventData</typeparam>
    public interface IEventListener<T> where T : EventData
    {
        /// <summary>
        /// 监听器优先级
        /// </summary>
        /// <remarks>
        /// 数值越大优先级越高，越先接收事件
        /// 用于控制多个监听器的执行顺序
        /// 优先级相同时按注册顺序执行
        /// </remarks>
        int Priority { get; }
        
        /// <summary>
        /// 事件过滤器
        /// </summary>
        /// <param name="eventData">事件数据实例</param>
        /// <returns>返回 true 表示接收此事件，false 表示忽略</returns>
        /// <remarks>
        /// 可在监听器中实现自定义过滤逻辑
        /// 例如：只处理特定条件的事件
        /// 返回 false 时 OnEvent 不会被调用
        /// </remarks>
        bool OnFilter(T eventData);
        
        /// <summary>
        /// 事件处理方法
        /// </summary>
        /// <param name="eventData">事件数据实例</param>
        /// <remarks>
        /// 当过滤器返回 true 时调用此方法
        /// 在此处编写事件响应逻辑
        /// 可设置 eventData.IsBreak = true 中断后续监听器
        /// </remarks>
        void OnEvent(T eventData);
    }
}