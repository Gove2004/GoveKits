namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 事件数据基类
    /// </summary>
    public abstract class EventData : IPoolable
    {
        public bool IsBreak { get; set; }
        public abstract void OnRecycle();
    }
}