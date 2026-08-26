namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 可被对象池系统管理的接口。
    /// 对象被归还池时调用 OnRecycle 以重置其状态，确保下次取出时是干净的空壳。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 对象被归还池时调用，用于重置状态。
        /// </summary>
        void OnRecycle();
    }
}