namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 事件监听器泛型接口。
    /// Priority 越大越先执行；OnFilter 返回 false 则跳过该监听器。
    /// </summary>
    public interface IEventListener<TEvent> where TEvent : EventData
    {
        /// <summary>
        /// 监听器优先级，数值越大越先执行。
        /// </summary>
        int Priority { get; }
        /// <summary>
        /// 过滤器，返回 false 则跳过该监听器的 OnEvent 调用。
        /// </summary>
        bool OnFilter(TEvent eventData);
        /// <summary>
        /// 接收到事件时的回调方法。
        /// </summary>
        void OnEvent(TEvent eventData);
    }
}