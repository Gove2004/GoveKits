namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 对象池基础接口（非泛型）
    /// </summary>
    /// <remarks>
    /// 只定义核心生命周期方法，无需泛型参数
    /// 便于在 Dictionary 中统一存储不同类型的池实例
    /// 使用多态管理，避免类型擦除问题
    /// </remarks>
    public interface IPool
    {
        /// <summary>
        /// 当前缓存的对象数量
        /// </summary>
        int CachedCount { get; }
        
        /// <summary>
        /// 池最大容量限制
        /// </summary>
        int MaxSize { get; }
        
        /// <summary>
        /// 预热池，预先创建指定数量的对象
        /// </summary>
        /// <param name="count">预热数量</param>
        void Warmup(int count);
        
        /// <summary>
        /// 清空池中所有缓存对象
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 对象池泛型接口
    /// </summary>
    /// <remarks>
    /// 继承自 IPool，增加类型安全的获取和归还方法
    /// 强制类型约束，消除运行时类型转换（cast）
    /// 提供编译时类型检查，减少错误
    /// </remarks>
    /// <typeparam name="T">池化对象类型</typeparam>
    public interface IPool<T> : IPool
    {
        /// <summary>
        /// 从池中获取一个对象
        /// </summary>
        /// <returns>池化对象实例</returns>
        T Get();
        
        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        /// <param name="item">要归还的对象实例</param>
        void Return(T item);
    }
}