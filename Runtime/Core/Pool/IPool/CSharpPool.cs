using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 纯 C# 对象池实现类
    /// </summary>
    /// <remarks>
    /// 适用于普通 C# 类对象的池化管理
    /// 使用 Stack 数据结构实现 LIFO（后进先出）策略
    /// 要求泛型类型 T 必须实现 IPoolable 接口且有无参构造函数
    /// </remarks>
    /// <typeparam name="T">池化类型，必须为 class 且实现 IPoolable</typeparam>
    public class CSharpPool<T> : IPool, IPool<T> where T : class, IPoolable, new()
    {
        /// <summary>
        /// 对象缓存栈
        /// 使用 Stack 实现后进先出，提高缓存命中率
        /// </summary>
        private readonly Stack<T> _stack;
        
        /// <summary>
        /// 当前缓存的对象数量（只读）
        /// </summary>
        public int CachedCount => _stack.Count;
        
        /// <summary>
        /// 池最大容量限制（只读）
        /// 超过此数量的对象归还时会被直接销毁
        /// </summary>
        public int MaxSize { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxSize">池最大容量，默认 100</param>
        public CSharpPool(int maxSize = 100)
        {
            // 初始化栈，预设容量提升性能
            _stack = new Stack<T>(maxSize);
            MaxSize = maxSize;
        }

        /// <summary>
        /// 预热池
        /// 预先创建指定数量的对象并放入池中
        /// 避免运行时频繁创建对象产生 GC
        /// </summary>
        /// <param name="count">预热数量</param>
        public void Warmup(int count)
        {
            for (int i = 0; i < count && _stack.Count < MaxSize; i++)
            {
                // 创建对象后立即归还，触发 OnRecycle 初始化
                Return(Get());
            }
        }

        /// <summary>
        /// 清空池中所有缓存对象
        /// 注意：不会调用 OnRecycle，直接清空栈
        /// </summary>
        public void Clear() => _stack.Clear();

        /// <summary>
        /// 从池中获取一个对象
        /// 优先从缓存栈中弹出，栈空时创建新对象
        /// </summary>
        /// <returns>池化对象实例</returns>
        public T Get()
        {
            // 栈中有缓存则弹出，否则创建新实例
            return _stack.Count > 0 ? _stack.Pop() : new T();
        }

        /// <summary>
        /// 将对象归还到池中
        /// 调用 OnRecycle 重置对象状态后压入栈
        /// 超过最大容量时对象会被丢弃（由 GC 回收）
        /// </summary>
        /// <param name="item">要归还的对象实例</param>
        public void Return(T item)
        {
            // 空值检查，避免空引用异常
            if (item == null) return;
            
            // 容量检查，超过最大容量不缓存
            if (_stack.Count < MaxSize)
            {
                // 调用对象的重置方法
                item.OnRecycle();
                // 压入栈中等待下次复用
                _stack.Push(item);
            }
        }
    }
}