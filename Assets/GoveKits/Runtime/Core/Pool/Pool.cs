using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GoveKits.Runtime.Core.Pool
{
    /// <summary>
    /// 所有池的共同父类，统一管理容量、清理与预热能力。
    /// </summary>
    public abstract class BasePool
    {
        public abstract object Get();
        public abstract void Return(object item);
        public abstract void Clear();
    }


#region C# Pool

    // C# 实例池
    public class CSharpPool<T> : BasePool where T : class, IPoolable, new()
    {
        private readonly Stack<T> _stack;
        private int _maxSize;

        public CSharpPool(int maxSize = 64)
        {
            _maxSize = maxSize;
            _stack = new Stack<T>();
        }


        public void Warmup(int count = 8)
        {
            for (int i = 0; i < count; i++)
            {
                if (_stack.Count >= _maxSize) break;
                _stack.Push(new T());
            }
        }

        public T GetTyped()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : new T();
            return item;
        }
    
        public override object Get() => GetTyped();

        public void ReturnTyped(T item)
        {
            if (item == null) return;
            if (_maxSize >= 0 && _stack.Count >= _maxSize) return;
            _stack.Push(item);
        }

        public override void Return(object item)
        {
            if (item is T typedItem)
            {
                ReturnTyped(typedItem);
            }
        }

        public override void Clear() => _stack.Clear();
    }

#endregion

#region GameObject Pool

    public class PoolRecord : MonoBehaviour
    {
        public GameObjectPool SourcePool { get; set; }
    }

    // Unity 对象池管理
    public class GameObjectPool : BasePool
    {
        private readonly IObjectPool<GameObject> _pool;

        public GameObjectPool(GameObject prefab, int count, int maxSize)
        {
            _pool = new ObjectPool<GameObject>(
                createFunc: () => {
                    var obj = UnityEngine.Object.Instantiate(prefab);
                    obj.AddComponent<PoolRecord>().SourcePool = this;
                    return obj;
                },
                actionOnGet: obj => {
                    obj.SetActive(true);
                    foreach (var p in obj.GetComponents<IPoolable>())
                    {
                        p.OnGetFromPool();
                    }
                },
                actionOnRelease: obj => {
                    foreach (var p in obj.GetComponents<IPoolable>())
                    {
                        p.OnReturnToPool();
                    }
                    obj.SetActive(false);
                },
                actionOnDestroy: UnityEngine.Object.Destroy,
                defaultCapacity: count,
                maxSize: maxSize
            );
        }

        public GameObject GetTyped() => _pool.Get();

        public override object Get() => GetTyped();

        public override void Return(object item)
        {
            if (item is GameObject obj)
            {
                _pool.Release(obj);
            }
        }

        public override void Clear() => _pool.Clear();
    }

#endregion
}