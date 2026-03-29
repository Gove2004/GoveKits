using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// Unity GameObject 对象池实现类
    /// </summary>
    /// <remarks>
    /// 专门用于管理 Unity GameObject 的池化
    /// 支持预制体（Prefab）实例的复用和销毁
    /// 自动管理对象的激活/禁用状态
    /// 通过 PoolRecord 组件追踪对象来源池
    /// </remarks>
    public class GameObjectPool : IPool, IPool<GameObject>
    {
        /// <summary>
        /// 预制体模板引用
        /// 用于创建新的 GameObject 实例
        /// </summary>
        private GameObject _prefab;
        
        /// <summary>
        /// 对象缓存栈
        /// 存储已禁用等待复用的 GameObject
        /// </summary>
        private readonly Stack<GameObject> _stack = new();
        
        /// <summary>
        /// 当前缓存的对象数量（只读）
        /// </summary>
        public int CachedCount => _stack.Count;
        
        /// <summary>
        /// 池最大容量限制（只读）
        /// </summary>
        public int MaxSize { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prefab">预制体模板</param>
        /// <param name="maxSize">池最大容量</param>
        public GameObjectPool(GameObject prefab, int maxSize)
        {
            _prefab = prefab;
            MaxSize = maxSize;
        }

        /// <summary>
        /// 预热池
        /// 预先创建指定数量的 GameObject 实例
        /// </summary>
        /// <param name="count">预热数量</param>
        public void Warmup(int count)
        {
            for (int i = 0; i < count && _stack.Count < MaxSize; i++)
            {
                Return(Get());
            }
        }

        /// <summary>
        /// 清空池中所有缓存对象
        /// 遍历栈中所有对象，禁用并销毁
        /// </summary>
        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var obj = _stack.Pop();
                obj.SetActive(false);
                GameObject.Destroy(obj);
            }
        }

        /// <summary>
        /// 从池中获取一个 GameObject 实例
        /// 优先从缓存栈中获取有效对象
        /// 缓存为空或对象已销毁时实例化新对象
        /// </summary>
        /// <returns>激活状态的 GameObject 实例</returns>
        public GameObject Get()
        {
            // 遍历缓存栈，查找有效对象
            while (_stack.Count > 0)
            {
                var obj = _stack.Pop();
                if (obj != null)
                {
                    // 激活对象并返回
                    obj.SetActive(true);
                    return obj;
                }
            }

            // 缓存为空，实例化新对象
            var newObj = GameObject.Instantiate(_prefab);
            
            // 添加或获取 PoolRecord 组件，记录来源池
            var record = newObj.GetComponent<PoolRecord>();
            if (record == null) record = newObj.AddComponent<PoolRecord>();
            record.SourcePool = this;
            
            return newObj;
        }

        /// <summary>
        /// 将 GameObject 实例归还到池中
        /// 调用重置逻辑后禁用对象并压入栈
        /// 超过容量限制时直接销毁对象
        /// </summary>
        /// <param name="item">要归还的 GameObject 实例</param>
        public void Return(GameObject item)
        {
            if (item == null) return;
            
            if (_stack.Count < MaxSize)
            {
                // 重置对象状态
                RecycleGameObject(item);
                // 禁用对象
                item.SetActive(false);
                // 压入缓存栈
                _stack.Push(item);
            }
            else
            {
                // 超过容量，重置后销毁
                RecycleGameObject(item);
                GameObject.Destroy(item);
            }
        }
        
        /// <summary>
        /// 缓冲列表，避免每次 Return 都调用 GetComponentsInChildren 产生 GC Allocation
        /// 静态复用，减少内存分配
        /// </summary>
        private static readonly List<IPoolable> _tempPoolables = new List<IPoolable>();
        
        /// <summary>
        /// 递归重置 GameObject 及其子对象上的所有 IPoolable 组件
        /// </summary>
        /// <param name="obj">要重置的 GameObject</param>
        private static void RecycleGameObject(GameObject obj)
        {
            // 获取所有子对象上的 IPoolable 组件
            obj.GetComponentsInChildren<IPoolable>(true, _tempPoolables);
            
            // 遍历并调用每个组件的重置方法
            foreach (var p in _tempPoolables) p.OnRecycle();
            
            // 清空临时列表，供下次复用
            _tempPoolables.Clear();
        }
    }

    /// <summary>
    /// 池记录组件
    /// 挂载到池化 GameObject 上，记录其来源池
    /// </summary>
    /// <remarks>
    /// PoolCore.Return(GameObject) 会通过这个记录把对象送回正确的池
    /// 确保对象归还到创建它的原始预制体池
    /// </remarks>
    public class PoolRecord : MonoBehaviour
    {
        /// <summary>
        /// 记录该对象最初归属的 GameObjectPool
        /// </summary>
        public GameObjectPool SourcePool { get; set; }
    }
}