namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件数据基类
    /// </summary>
    /// <remarks>
    /// 所有事件类型必须继承此类
    /// 实现 IPoolable 接口，支持对象池复用，减少 GC 分配
    /// 提供事件中断控制功能
    /// </remarks>
    public abstract class EventData : IPoolable
    {
        /// <summary>
        /// 事件中断标记
        /// </summary>
        /// <remarks>
        /// 设置为 true 时，后续监听器将不再接收此事件
        /// 用于实现事件拦截、优先级阻断等功能
        /// 在事件回收前会自动重置为 false
        /// </remarks>
        public bool IsBreak { get; set; }
        
        /// <summary>
        /// 对象回收时调用
        /// </summary>
        /// <remarks>
        /// 子类必须实现此方法，用于重置事件数据状态
        /// 在事件分发结束后由 PoolCore 自动调用
        /// 确保事件对象可安全复用于下一次分发
        /// </remarks>
        public abstract void OnRecycle();
    }
}