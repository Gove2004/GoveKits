namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件监听器接口
    /// </summary>
    public interface IEventListener<TEvent> where TEvent : EventData
    {
        int Priority { get; }
        bool OnFilter(TEvent eventData);
        void OnEvent(TEvent eventData);
    }
}