using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 纯 C# 对象池实现类
    /// </summary>
    public class CSharpPool<T> : IPool, IPool<T> where T : class, IPoolable, new()
    {
        private readonly Stack<T> _stack;
        
        public int CachedCount => _stack.Count;
        
        public int MaxSize { get; private set; }

        public CSharpPool(int maxSize = 100)
        {
            _stack = new Stack<T>(maxSize);
            MaxSize = maxSize;
        }

        public void Warmup(int count)
        {
            for (int i = 0; i < count && _stack.Count < MaxSize; i++)
            {
                Return(Get());
            }
        }

        public void Clear() => _stack.Clear();

        public T Get()
        {
            return _stack.Count > 0 ? _stack.Pop() : new T();
        }

        public void Return(T item)
        {
            if (item == null) return;
            
            if (_stack.Count < MaxSize)
            {
                item.OnRecycle();
                _stack.Push(item);
            }
        }
    }
}