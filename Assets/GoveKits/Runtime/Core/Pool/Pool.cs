using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GoveKits.Runtime.Core.Pool
{
    /// <summary>
    /// 所有池的共同父类。
    /// </summary>
    /// <remarks>
    /// 这个抽象层的目的不是暴露完整泛型能力，而是为了在 PoolCore 中统一存放不同类型的池实例。
    /// 也就是说，PoolCore 内部用 BasePool 做“统一容器”，具体的取值和归还仍由子类完成。
    /// </remarks>
    public abstract class BasePool
    {
        /// <summary>
        /// 以 object 形式取出一个对象。
        /// </summary>
        public abstract object Get();

        /// <summary>
        /// 以 object 形式归还一个对象。
        /// </summary>
        public abstract void Return(object item);

        /// <summary>
        /// 清空池内缓存对象。
        /// </summary>
        public abstract void Clear();
    }


#region C# Pool

    /// <summary>
    /// 纯 C# 对象池。
    /// </summary>
    /// <typeparam name="T">
    /// 被池化的对象类型。
    /// 必须是引用类型、实现 <see cref="IPoolable"/>、并且具备无参构造函数。
    /// </typeparam>
    /// <remarks>
    /// 适用于不依赖 Unity 生命周期的对象，例如：
    /// - 战斗临时数据
    /// - 运行时命令对象
    /// - 路径节点、伤害结算对象、Buff 数据容器
    /// </remarks>
    public class CSharpPool<T> : BasePool where T : class, IPoolable, new()
    {
        private readonly Stack<T> _stack;
        private int _maxSize;

        /// <summary>
        /// 创建一个纯 C# 对象池。
        /// </summary>
        /// <param name="maxSize">池最大缓存数量。</param>
        public CSharpPool(int count, int maxSize)
        {
            _maxSize = maxSize;
            _stack = new Stack<T>();

            for (int i = 0; i < count; i++)
            {
                if (_stack.Count >= _maxSize) break;
                _stack.Push(new T());
            }
        }

        /// <summary>
        /// 以强类型方式取出对象。
        /// </summary>
        /// <returns>池中已有对象，或在池为空时新创建的对象。</returns>
        public T GetTyped()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : new T();
            return item;
        }
    
        public override object Get() => GetTyped();

        /// <summary>
        /// 以强类型方式归还对象。
        /// </summary>
        /// <param name="item">要归还的对象。</param>
        /// <remarks>
        /// 当池已满时，多余对象会被直接丢弃，不会继续缓存。
        /// </remarks>
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
        /// <summary>
        /// 记录该对象最初归属的 GameObjectPool。
        /// </summary>
        /// <remarks>
        /// PoolCore.Return(GameObject) 会通过这个记录把对象送回正确的池。
        /// </remarks>
        public GameObjectPool SourcePool { get; set; }
    }

    /// <summary>
    /// Unity GameObject 对象池。
    /// </summary>
    /// <remarks>
    /// 基于 UnityEngine.Pool.ObjectPool 实现，负责：
    /// - 实例化 prefab
        /// - 取出时激活对象
        /// - 归还时调用所有 IPoolable 组件的 OnRecycle 并禁用对象
    /// </remarks>
    public class GameObjectPool : BasePool
    {
        private readonly IObjectPool<GameObject> _pool;

        /// <summary>
        /// 创建一个 GameObject 池。
        /// </summary>
        /// <param name="prefab">用于实例化的 prefab。</param>
        /// <param name="count">默认容量，用于减少内部扩容次数。</param>
        /// <param name="maxSize">池允许缓存的最大对象数。</param>
        public GameObjectPool(GameObject prefab, int count, int maxSize)
        {
            _pool = new ObjectPool<GameObject>(
                createFunc: () => {
                    // 每次池内没有可用对象时，按 prefab 克隆一个新实例。
                    var obj = UnityEngine.Object.Instantiate(prefab);

                    // 给实例附加 PoolRecord，用于后续通过 PoolCore.Return(obj) 定位来源池。
                    obj.AddComponent<PoolRecord>().SourcePool = this;
                    return obj;
                },
                actionOnGet: obj => {
                    // 取出时只负责激活对象，使用态初始化交给业务代码自己决定。
                    obj.SetActive(true);
                },
                actionOnRelease: obj => {
                    // 归还时先通知对象做清理，再关闭物体，避免残留运行状态。
                    foreach (var p in obj.GetComponents<IPoolable>())
                    {
                        p.OnRecycle();
                    }
                    obj.SetActive(false);
                },
                // 当池已满、对象无法继续缓存时，由 ObjectPool 触发销毁。
                actionOnDestroy: UnityEngine.Object.Destroy,
                defaultCapacity: count,
                maxSize: maxSize
            );
        }

        /// <summary>
        /// 以强类型方式取出一个 GameObject。
        /// </summary>
        public GameObject GetTyped() => _pool.Get();

        public override object Get() => GetTyped();

        public override void Return(object item)
        {
            if (item is GameObject obj)
            {
                _pool.Release(obj);
            }
        }

        /// <summary>
        /// 清空池中当前缓存的所有 GameObject。
        /// </summary>
        public override void Clear() => _pool.Clear();
    }

#endregion
}