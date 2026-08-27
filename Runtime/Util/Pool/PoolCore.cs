using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 对象池模块统一入口。
    /// 管理 CSharp 对象池（针对实现了 IPoolable 的纯托管对象）
    /// 和 GameObject 对象池的创建、获取、归还、清理和生命周期。
    /// </summary>
    public static class PoolCore
    {
        /// <summary>CSharp 对象池注册表，以对象类型为键，同类型仅一个池。</summary>
        private static readonly Dictionary<Type, IPool> csharpPools = new();
        /// <summary>GameObject 对象池注册表，以预制体 InstanceID 为键。</summary>
        private static readonly Dictionary<int, GameObjectPool> gameObjectPools = new();

        #region CSharpPool

        /// <summary>
        /// 创建或获取指定类型的 CSharp 对象池。
        /// 同一类型只会创建一个池实例，后续调用返回已有的池。
        /// </summary>
        /// <param name="count">预热数量，即预先创建的缓存对象数</param>
        /// <param name="maxSize">池的最大容量，超出容量的对象将被销毁</param>
        /// <typeparam name="T">必须是无参构造函数且实现 IPoolable 的引用类型</typeparam>
        /// <returns>对应类型的 CSharpPool 实例</returns>
        public static CSharpPool<T> Create<T>(int count = 8, int maxSize = 64)
            where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (!csharpPools.TryGetValue(type, out var pool))
            {
                pool = new CSharpPool<T>(maxSize);
                pool.Warmup(count);
                csharpPools[type] = pool;
            }
            return (CSharpPool<T>)pool;
        }

        /// <summary>
        /// 从指定类型的对象池中获取一个实例。
        /// 如果该类型尚未创建池，则自动创建并预热。
        /// </summary>
        /// <typeparam name="T">要获取的池化对象类型</typeparam>
        /// <returns>可用的对象实例</returns>
        public static T Get<T>() where T : class, IPoolable, new()
        {
            return Create<T>().Get();
        }

        /// <summary>
        /// 将一个对象归还到其类型的对象池中。
        /// 归还时会自动调用对象的 OnRecycle 方法重置状态。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="item">要归还的对象，为 null 时静默忽略</param>
        public static void Return<T>(T item) where T : class, IPoolable, new()
        {
            if (item == null) return;
            Create<T>().Return(item);
        }

        /// <summary>
        /// 清空并注销指定类型的 CSharp 对象池及其所有缓存对象。
        /// </summary>
        /// <typeparam name="T">要清理的池化对象类型</typeparam>
        public static void Clear<T>() where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (csharpPools.TryGetValue(type, out var pool))
            {
                pool.Clear();
                csharpPools.Remove(type);
            }
        }

        #endregion

        #region GameObjectPool

        /// <summary>
        /// 创建或获取指定预制体的 GameObject 对象池。
        /// 同一 prefab 只会创建一个池实例，后续调用返回已有的池。
        /// </summary>
        /// <param name="prefab">用于实例化的预制体</param>
        /// <param name="count">预热数量</param>
        /// <param name="maxSize">池的最大容量</param>
        /// <returns>对应的 GameObjectPool 实例</returns>
        public static GameObjectPool Create(GameObject prefab, int count = 8, int maxSize = 64)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            int id = prefab.GetInstanceID();
            if (!gameObjectPools.TryGetValue(id, out var pool))
            {
                pool = new GameObjectPool(prefab, maxSize: maxSize);
                pool.Warmup(count);
                gameObjectPools[id] = pool;
            }
            return pool;
        }

        /// <summary>
        /// 从指定预制体的对象池中获取一个 GameObject 实例。
        /// </summary>
        /// <param name="prefab">用于获取的预制体引用</param>
        /// <returns>可用的 GameObject 实例</returns>
        public static GameObject Get(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return Create(prefab).Get();
        }

        /// <summary>
        /// 归还一个 GameObject 到其所属的对象池。
        /// 若该对象未关联任何池（缺少 PoolRecord），则直接销毁它。
        /// </summary>
        /// <param name="obj">要归还的 GameObject</param>
        public static void Return(GameObject obj)
        {
            if (obj == null) return;

            var record = obj.GetComponent<PoolRecord>();
            if (record == null || record.SourcePool == null)
            {
                GameObject.Destroy(obj);
                return;
            }

            record.SourcePool.Return(obj);
        }

        /// <summary>
        /// 清空并注销指定预制体的 GameObject 对象池。
        /// </summary>
        /// <param name="prefab">要清理的预制体引用</param>
        public static void Clear(GameObject prefab)
        {
            if (prefab == null) return;

            int id = prefab.GetInstanceID();
            if (gameObjectPools.TryGetValue(id, out var pool))
            {
                pool.Clear();
                gameObjectPools.Remove(id);
            }
        }

        #endregion

        /// <summary>
        /// 清空所有对象池（包括 CSharp 池和 GameObject 池），销毁所有缓存对象并释放资源。
        /// 通常在场景切换或应用退出时调用。
        /// </summary>
        public static void Close()
        {
            foreach (var pool in csharpPools.Values) pool.Clear();
            foreach (var pool in gameObjectPools.Values) pool.Clear();
            csharpPools.Clear();
            gameObjectPools.Clear();
        }
    }
}
