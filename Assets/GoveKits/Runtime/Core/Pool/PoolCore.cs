using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Core.Pool
{
    public static class PoolCore
    {
        private static readonly Dictionary<Type, BasePool> _csharpPools = new();
        private static readonly Dictionary<int, GameObjectPool> _gameObjectPools = new();

#region C# Pool

        public static CSharpPool<T> Create<T>(int count = 8, int maxSize = 64) where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (!_csharpPools.TryGetValue(type, out var pool))
            {
                pool = new CSharpPool<T>(maxSize);
                _csharpPools[type] = pool;
                ((CSharpPool<T>)pool).Warmup(count);
            }
            return (CSharpPool<T>)pool;
        }

        public static T Get<T>() where T : class, IPoolable, new()
        {
            CSharpPool<T> pool = Create<T>();
            IPoolable item = pool.GetTyped();
            item.OnGetFromPool();
            return (T)item;
        }

        public static void Return<T>(T item) where T : class, IPoolable, new()
        {
            item.OnReturnToPool();
            CSharpPool<T> pool = Create<T>();
            pool.ReturnTyped(item);
        }

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

#region GameObject Pool

        private static void CheckPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException($"Prefab {prefab?.name} is null.");
            }
            if (prefab.GetComponent<IPoolable>() == null)
            {
                throw new ArgumentException($"Prefab {prefab.name} must have a IPoolable component.");
            }
        }

        public static GameObjectPool Create(GameObject prefab, int count = 0, int maxSize = 64)
        {
            CheckPrefab(prefab);
            int id = prefab.GetInstanceID();
            if (!_gameObjectPools.TryGetValue(id, out var pool))
            {
                pool = new GameObjectPool(prefab, count: count, maxSize: maxSize);
                _gameObjectPools[id] = pool;
            }
            return pool;
        }

        public static GameObject Get(GameObject prefab)
        {
            CheckPrefab(prefab);
            GameObjectPool pool = Create(prefab);
            return pool.GetTyped();
        }

        public static void Return(GameObject obj)
        {
            CheckPrefab(obj);
            PoolRecord record = obj.GetComponent<PoolRecord>();
            record?.SourcePool?.Return(obj);
        }

        public static void Clear(GameObject prefab)
        {
            CheckPrefab(prefab);
            int id = prefab.GetInstanceID();
            if (_gameObjectPools.TryGetValue(id, out var pool))
            {
                pool.Clear();
                _gameObjectPools.Remove(id);
            }
        }


#endregion

        public static void ClearAll()
        {
            foreach (var pool in _csharpPools.Values) pool.Clear();
            foreach (var pool in _gameObjectPools.Values) pool.Clear();
            _csharpPools.Clear();
            _gameObjectPools.Clear();
        }
    }
}