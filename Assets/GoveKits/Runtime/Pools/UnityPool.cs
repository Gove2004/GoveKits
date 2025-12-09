using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace GoveKits.Pools
{
    /// <summary>
    /// 【内部核心】管理所有 Unity GameObject 池的单例。
    /// 它使用一个字典来为每个不同的 Prefab 动态地创建和维护一个专属的对象池。
    /// </summary>
    internal static class UnityPool
    {
        // 字典，用于存储每个 Prefab 对应的池。
        // Key: Prefab 的唯一实例ID (GetInstanceID)，保证每个 Prefab 只有一个池。
        // Value: 专用于该 Prefab 的对象池。
        private static Dictionary<int, IObjectPool<GameObject>> _pools = new Dictionary<int, IObjectPool<GameObject>>();
        
        // 一个在场景中统一存放所有已回收对象的根节点，以保持 Hierarchy 整洁。
        private static Transform _root;

        /// <summary>
        /// 初始化用于存放回收对象的根节点。
        /// </summary>
        private static void InitializeRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("[Pool Root]").transform;
                Object.DontDestroyOnLoad(_root.gameObject);
            }
        }

        /// <summary>
        /// 根据传入的 Prefab，获取其对应的池。如果池不存在，则动态创建一个新池。
        /// </summary>
        private static IObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            int key = prefab.GetInstanceID();

            // 如果字典中还没有这个 Prefab 的池，就创建一个。
            if (!_pools.ContainsKey(key))
            {
                InitializeRoot();

                // 定义如何创建、获取、释放和销毁池中对象的回调函数。
                var newPool = new ObjectPool<GameObject>(
                    // 1. 创建新对象的方法
                    createFunc: () => {
                        var instance = Object.Instantiate(prefab);
                        // 【关键】在新创建的实例上附加 PoolRecord 组件，并让它“记住”自己的池。
                        instance.AddComponent<PoolRecord>().Pool = _pools[key];
                        return instance;
                    },
                    // 2. 从池中获取对象时的操作
                    actionOnGet: (instance) => {
                        instance.transform.SetParent(null); // 从根节点下移出，回归世界空间
                        instance.gameObject.SetActive(true);
                    },
                    // 3. 回收对象进池时的操作
                    actionOnRelease: (instance) => {
                        instance.transform.SetParent(_root); // 放入根节点下，保持场景整洁
                        instance.gameObject.SetActive(false);
                    },
                    // 4. 当池已满，销毁多余对象时的操作
                    actionOnDestroy: (instance) => Object.Destroy(instance),
                    collectionCheck: PoolConfig.EnableUnityPoolCollectionCheck, // 开启集合检查，防止同一对象被重复回收
                    defaultCapacity: PoolConfig.DefaultUnityPoolCapacity,
                    maxSize: PoolConfig.MaxUnityPoolSize
                );
                
                // 将新创建的池添加到字典中。
                _pools.Add(key, newPool);
            }

            return _pools[key];
        }

        /// <summary>
        /// 获取一个 GameObject 实例。
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();

            // 主动调用实例上 IPoolable 接口的 OnGet 方法
            var poolable = instance.GetComponent<IPoolable>();
            return instance;
        }

        /// <summary>
        /// 回收一个 GameObject 实例。
        /// </summary>
        public static void Recycle(GameObject instance)
        {
            // 通过 PoolRecord 组件找到它应该回收到哪个池。
            var record = instance.GetComponent<PoolRecord>();
            if (record == null || record.Pool == null)
            {
                Debugger.Logger.LogWarning($"对象 '{instance.name}' 不是由池创建的或已被销毁，将执行 Object.Destroy()。");
                Object.Destroy(instance);
                return;
            }

            // 主动调用实例上 IPoolable 接口的 OnRecycle 方法
            var poolable = instance.GetComponent<IPoolable>();
            poolable?.OnRecycle();

            // 将对象交还给它所属的池。
            record.Pool.Release(instance);
        }
    }
}