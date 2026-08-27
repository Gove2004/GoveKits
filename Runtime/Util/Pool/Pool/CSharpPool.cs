using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 纯 C# 泛型对象池，适用于实现了 IPoolable 接口的纯托管引用类型（如数据对象、事件等）。
    /// 通过 PoolCore 创建与使用（自动预热、按类型单例），通常无需直接实例化。
    /// </summary>
    /// <typeparam name="T">必须是无参构造函数且实现 IPoolable 的引用类型</typeparam>
    public class CSharpPool<T> : IPool, IPool<T> where T : class, IPoolable, new()
    {
        private readonly Stack<T> stack;

        /// <summary>池中当前缓存的对象数量。</summary>
        public int Count => stack.Count;
        /// <summary>对象池的最大容量。</summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// 创建指定容量的 CSharp 对象池。
        /// </summary>
        /// <param name="maxSize">池的最大容量</param>
        public CSharpPool(int maxSize = 100)
        {
            stack = new Stack<T>(maxSize);
            Capacity = maxSize;
        }

        /// <summary>
        /// 预热池，预先创建并缓存指定数量的对象。
        /// </summary>
        /// <param name="count">要预热的对象数量</param>
        public void Warmup(int count)
        {
            for (int i = 0; i < count && stack.Count < Capacity; i++)
                Return(Get());
        }

        /// <summary>
        /// 从池中获取一个可用对象。池空时自动创建新实例。
        /// </summary>
        /// <returns>可用的对象实例</returns>
        public T Get()
        {
            return stack.Count > 0 ? stack.Pop() : new T();
        }

        /// <summary>
        /// 将一个对象归还到池中。
        /// 归还时自动调用 OnRecycle 重置对象状态；超出容量时静默丢弃。
        /// </summary>
        /// <param name="item">要归还的对象，为 null 时静默忽略</param>
        public void Return(T item)
        {
            if (item == null) return;

            if (stack.Count < Capacity)
            {
                item.OnRecycle();
                stack.Push(item);
            }
        }

        /// <summary>清空池中所有缓存对象。</summary>
        public void Clear() => stack.Clear();
    }
}