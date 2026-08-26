namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 对象池的非泛型基础接口，提供通用的计数、容量、预热和清理能力。
    /// </summary>
    public interface IPool
    {
        /// <summary>池中当前缓存的对象数量。</summary>
        int Count { get; }
        /// <summary>对象池的最大容量，超出部分将在归还时被销毁。</summary>
        int Capacity { get; }
        /// <summary>
        /// 预热池，预先创建并缓存指定数量的对象。
        /// </summary>
        /// <param name="count">要预热的对象数量</param>
        void Warmup(int count);
        /// <summary>清空池中所有缓存对象。</summary>
        void Clear();
    }

    /// <summary>
    /// 对象池的泛型接口，继承 IPool 并增加类型安全的获取和归还方法。
    /// </summary>
    /// <typeparam name="T">池化对象的类型</typeparam>
    public interface IPool<T> : IPool
    {
        /// <summary>从池中获取一个可用对象，池空时由实现自行创建新实例。</summary>
        T Get();
        /// <summary>
        /// 将一个对象归还到池中。
        /// 超出容量时将拒绝归还或由实现自行决定处理方式。
        /// </summary>
        /// <param name="item">要归还的对象</param>
        void Return(T item);
    }
}