namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件数据抽象基类，继承自 IPoolable 以便通过池系统管理生命周期。
    /// 发布前将 IsBreak 设为 true 可中断后续监听器的执行。
    /// 派生类需在 OnRecycle 中重置自身状态。
    /// </summary>
    public abstract class EventData : IPoolable
    {
        /// <summary>
        /// 是否中断事件传播。设为 true 时，总线将跳过后续监听器。
        /// </summary>
        public bool IsBreak { get; set; }
        /// <summary>
        /// 对象被归还池时调用，派生类在此重置状态。
        /// </summary>
        public abstract void OnRecycle();
    }
}