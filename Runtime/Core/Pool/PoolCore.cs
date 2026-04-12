using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 对象池系统总入口
    /// </summary>
    public static class PoolCore
    {
        private static readonly Dictionary<Type, IPool> _csharpPools = new();
        
        private static readonly Dictionary<int, GameObjectPool> _gameObjectPools = new();

        #region C# 对象池管理

        /// <summary>
        /// 创建一个C#对象池
        /// </summary>
        public static CSharpPool<T> Create<T>(int count = 8, int maxSize = 64) 
            where T : class, IPoolable, new()
        {
            var type = typeof(T);
            
            if (!_csharpPools.TryGetValue(type, out var pool))
            {
                pool = new CSharpPool<T>(maxSize);
                pool.Warmup(count);
                _csharpPools[type] = pool;
            }
            
            return (CSharpPool<T>)pool;
        }

        /// <summary>
        /// 获取一个C#对象
        /// </summary>
        public static T Get<T>() where T : class, IPoolable, new()
        {
            return Create<T>().Get();
        }

        /// <summary>
        /// 归还一个C#对象
        /// </summary>
        public static void Return<T>(T item) where T : class, IPoolable, new()
        {
            if (item == null) return;
            Create<T>().Return(item);
        }

        /// <summary>
        /// 清空一个C#对象池
        /// </summary>
        public static void Clear<T>() where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (_csharpPools.TryGetValue(type, out var pool))
            {
                pool.Clear();
                _csharpPools.Remove(type);
            }
        }

        #endregion

        #region GameObject 对象池管理

        /// <summary>
        /// 创建一个GameObject对象池
        /// </summary>
        public static GameObjectPool Create(GameObject prefab, int count = 8, int maxSize = 64)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            
            int id = prefab.GetInstanceID();
            
            if (!_gameObjectPools.TryGetValue(id, out var pool))
            {
                pool = new GameObjectPool(prefab, maxSize: maxSize);
                pool.Warmup(count);
                _gameObjectPools[id] = pool;
            }
            
            return pool;
        }

        /// <summary>
        /// 获取一个GameObject对象
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return Create(prefab).Get();
        }

        /// <summary>
        /// 归还一个GameObject对象
        /// </summary>
        public static void Return(GameObject obj)
        {
            if (obj == null) return;
            
            PoolRecord record = obj.GetComponent<PoolRecord>();
            
            if (record == null || record.SourcePool == null)
            {
                GameObject.Destroy(obj);
                return;
            }
            
            record.SourcePool.Return(obj);
        }

        /// <summary>
        /// 清空一个GameObject对象池
        /// </summary>
        public static void Clear(GameObject prefab)
        {
            int id = prefab.GetInstanceID();
            if (_gameObjectPools.TryGetValue(id, out var pool))
            {
                pool.Clear();
                _gameObjectPools.Remove(id);
            }
        }

        #endregion

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public static void Clear()
        {
            foreach (var pool in _csharpPools.Values) pool.Clear();
            foreach (var pool in _gameObjectPools.Values) pool.Clear();
            _csharpPools.Clear();
            _gameObjectPools.Clear();
        }
    }
}