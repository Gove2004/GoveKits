namespace GoveKits.Events
{
    /// <summary>
    /// 事件数据基类。
    /// <para>所有自定义事件必须继承此类，并实现 Reset 方法以支持对象池复用。</para>
    /// </summary>
    public abstract class EventInfo
    {
        /// <summary>
        /// 事件阻断标记。
        /// <para>如果在某个优先级的监听器中设为 true，后续优先级的监听器将不再收到此事件。</para>
        /// </summary>
        public bool IsStopped { get; set; } 

        /// <summary>
        /// [必须实现] 重置事件状态。
        /// <para>当事件从对象池取出或归还时调用。务必在此清空所有引用类型字段（如 GameObject, List），防止内存泄漏。</para>
        /// </summary>
        public abstract void Reset();
    }
}