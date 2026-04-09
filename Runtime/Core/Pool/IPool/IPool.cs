namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 对象池基础接口（非泛型）
    /// </summary>
    public interface IPool
    {
        int CachedCount { get; }
        
        int MaxSize { get; }
        
        void Warmup(int count);
        
        void Clear();
    }

    /// <summary>
    /// 对象池泛型接口
    /// </summary>
    public interface IPool<T> : IPool
    {
        T Get();
        
        void Return(T item);
    }
}