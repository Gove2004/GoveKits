namespace GoveKits.Pools
{
    /// <summary>
    /// 可池化对象的生命周期回调接口。
    /// 所有希望被池管理的对象都必须实现此接口。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 当对象被回收进池中时调用。
        /// 用于在对象被重用前进行必要的清理工作。
        /// </summary>
        void OnRecycle();
    }
}