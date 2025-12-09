using System.Collections.Generic;

namespace GoveKits.Pools
{
    /// <summary>
    /// 【内部核心】专用于纯 C# 类的对象池。
    /// 使用 Stack 实现，性能高，线程安全。
    /// </summary>
    internal static class CSharpPool<T> where T : class, IPoolable, new()
    {
        // 底层使用栈结构来存储对象，后进先出，效率极高。
        private readonly static Stack<T> _pool = new Stack<T>();
        // 用于保证多线程环境下的安全。
        private readonly static object _lock = new object();

        public static T Get()
        {
            T instance;
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    instance = _pool.Pop();
                }
                else
                {
                    // 如果池是空的，就创建一个新实例。
                    instance = new T();
                }
            }
            return instance;
        }

        public static void Recycle(T obj)
        {
            // 先调用对象的 OnRecycle 方法，进行清理。
            obj.OnRecycle();
            lock (_lock)
            {
                // 将对象压回栈中，等待下次使用。
                _pool.Push(obj);
            }
        }
    }
}