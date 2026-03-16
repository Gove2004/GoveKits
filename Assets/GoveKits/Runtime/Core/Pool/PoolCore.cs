using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GoveKits.Runtime.Core.Pool
{
    /// <summary>
    /// Pool 系统总入口。
    /// </summary>
    /// <remarks>
    /// 这个类负责维护所有类型对应的池实例，并对外提供统一的 Create / Get / Return / Clear 接口。
    /// 
    /// 当前实现分成两条线：
    /// 1. 纯 C# 对象池：按 Type 存放在 _csharpPools。
    /// 2. GameObject 池：按 prefab 的 InstanceID 存放在 _gameObjectPools。
    /// </remarks>
    public static class PoolCore
    {
        // 默认预热数量和最大缓存数量，可以根据项目需求调整
        private const int DefaultCSharpPoolCount = 0;
        private const int DefaultCSharpPoolMaxSize = 64;
        private const int DefaultGameObjectPoolCount = 0;
        private const int DefaultGameObjectPoolMaxSize = 64;


        private static readonly Dictionary<Type, BasePool> _csharpPools = new();
        private static readonly Dictionary<int, GameObjectPool> _gameObjectPools = new();

#if UNITY_EDITOR
        public static readonly List<string> PoolHistory = new();
        public static Action OnPoolSystemChanged;

        private static void RecordHistory(string message)
        {
            PoolHistory.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (PoolHistory.Count > 200)
            {
                PoolHistory.RemoveAt(PoolHistory.Count - 1);
            }
            OnPoolSystemChanged?.Invoke();
        }

        public readonly struct CSharpPoolDebugInfo
        {
            public CSharpPoolDebugInfo(string typeName, int cachedCount, int maxSize)
            {
                TypeName = typeName;
                CachedCount = cachedCount;
                MaxSize = maxSize;
            }

            public string TypeName { get; }
            public int CachedCount { get; }
            public int MaxSize { get; }
        }

        public readonly struct GameObjectPoolDebugInfo
        {
            public GameObjectPoolDebugInfo(int prefabId, string prefabName, int cachedCount, int activeCount, int allCount, int maxSize)
            {
                PrefabId = prefabId;
                PrefabName = prefabName;
                CachedCount = cachedCount;
                ActiveCount = activeCount;
                AllCount = allCount;
                MaxSize = maxSize;
            }

            public int PrefabId { get; }
            public string PrefabName { get; }
            public int CachedCount { get; }
            public int ActiveCount { get; }
            public int AllCount { get; }
            public int MaxSize { get; }
        }

        public static List<CSharpPoolDebugInfo> GetDebugCSharpPools()
        {
            return _csharpPools
                .Select(kvp => new CSharpPoolDebugInfo(kvp.Key.Name, kvp.Value.CachedCount, kvp.Value.MaxSize))
                .OrderByDescending(info => info.CachedCount)
                .ThenBy(info => info.TypeName, StringComparer.Ordinal)
                .ToList();
        }

        public static List<GameObjectPoolDebugInfo> GetDebugGameObjectPools()
        {
            return _gameObjectPools
                .Select(kvp => new GameObjectPoolDebugInfo(
                    kvp.Key,
                    kvp.Value.PrefabName,
                    kvp.Value.CachedCount,
                    kvp.Value.CountActive,
                    kvp.Value.CountAll,
                    kvp.Value.MaxSize))
                .OrderByDescending(info => info.CachedCount)
                .ThenBy(info => info.PrefabName, StringComparer.Ordinal)
                .ToList();
        }
#endif

#region C# Pool

        /// <summary>
        /// 创建或获取一个纯 C# 对象池。
        /// </summary>
        /// <typeparam name="T">池化类型。</typeparam>
        /// <param name="count">首次创建时的预热数量。</param>
        /// <param name="maxSize">池最大缓存数量。</param>
        /// <returns>类型 T 对应的对象池。</returns>
        /// <remarks>
        /// 如果该类型的池已经存在，则直接返回已有池，不会重复创建，也不会重新应用新的 count / maxSize 参数。
        /// </remarks>
        public static CSharpPool<T> Create<T>(int count = DefaultCSharpPoolCount, int maxSize = DefaultCSharpPoolMaxSize) where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (!_csharpPools.TryGetValue(type, out var pool))
            {
                pool = new CSharpPool<T>(count, maxSize);
                _csharpPools[type] = pool;
#if UNITY_EDITOR
                RecordHistory($"Create C# Pool | Type: {type.Name} | Prewarm: {count} | Max: {maxSize}");
#endif
            }
            return (CSharpPool<T>)pool;
        }

        /// <summary>
        /// 从纯 C# 对象池中取出一个对象。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>池中已有对象，或在池为空时新创建的对象。</returns>
        public static T Get<T>() where T : class, IPoolable, new()
        {
            CSharpPool<T> pool = Create<T>();
            IPoolable item = pool.GetTyped();
#if UNITY_EDITOR
            RecordHistory($"Get C# Item | Type: {typeof(T).Name} | Cached: {pool.CachedCount}/{pool.MaxSize}");
#endif
            return (T)item;
        }

        /// <summary>
        /// 归还一个纯 C# 对象到对应类型的池中。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="item">要归还的对象实例。</param>
        public static void Return<T>(T item) where T : class, IPoolable, new()
        {
            item.OnRecycle();
            CSharpPool<T> pool = Create<T>();
            pool.ReturnTyped(item);
#if UNITY_EDITOR
            RecordHistory($"Return C# Item | Type: {typeof(T).Name} | Cached: {pool.CachedCount}/{pool.MaxSize}");
#endif
        }

        /// <summary>
        /// 清空指定类型的纯 C# 对象池。
        /// </summary>
        /// <typeparam name="T">要清理的对象类型。</typeparam>
        public static void Clear<T>() where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (_csharpPools.TryGetValue(type, out var pool))
            {
                pool.Clear();
                _csharpPools.Remove(type);
#if UNITY_EDITOR
                RecordHistory($"Clear C# Pool | Type: {type.Name}");
#endif
            }
         }

#endregion

#region GameObject Pool

        /// <summary>
        /// 检查传入的 GameObject 是否满足池系统使用要求。
        /// </summary>
        /// <param name="prefab">要检查的 prefab 或实例对象。</param>
        /// <returns>校验通过返回 true；prefab 为 null 或缺少 IPoolable 组件时记录错误日志并返回 false。</returns>
        /// <remarks>
        /// 当前实现要求 GameObject 池对象至少挂有一个实现了 IPoolable 的组件，
        /// 这样在归还时，池系统才能正确派发 OnRecycle 回调。
        /// </remarks>
        private static bool CheckPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                GoveKitsCore.Log(nameof(PoolCore), "预制体不能为 null", logType: GoveKitsCore.LogType.Error);
                return false;
            }
            if (prefab.GetComponent<IPoolable>() == null)
            {
                GoveKitsCore.Log(nameof(PoolCore), $"预制体 {prefab.name} 必须包含一个 IPoolable 组件", logType: GoveKitsCore.LogType.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 创建或获取一个 GameObject 对象池。
        /// </summary>
        /// <param name="prefab">作为池模板的 prefab。</param>
        /// <param name="count">首次创建时的默认容量。</param>
        /// <param name="maxSize">池最大缓存数量。</param>
        /// <returns>该 prefab 对应的 GameObjectPool。</returns>
        /// <remarks>
        /// 池按 prefab 的 InstanceID 做唯一索引。
        /// 已创建过的 prefab 再次调用 Create 时，不会重新创建池，也不会更新参数。
        /// </remarks>
        public static GameObjectPool Create(GameObject prefab, int count = DefaultGameObjectPoolCount, int maxSize = DefaultGameObjectPoolMaxSize)
        {
            if (!CheckPrefab(prefab)) return null;
            int id = prefab.GetInstanceID();
            if (!_gameObjectPools.TryGetValue(id, out var pool))
            {
                pool = new GameObjectPool(prefab, count: count, maxSize: maxSize);
                _gameObjectPools[id] = pool;
#if UNITY_EDITOR
                RecordHistory($"Create GO Pool | Prefab: {prefab.name} | Id: {id} | Prewarm: {count} | Max: {maxSize}");
#endif
            }
            return pool;
        }

        /// <summary>
        /// 从指定 prefab 对应的池中取出一个实例。
        /// </summary>
        /// <param name="prefab">池模板 prefab。</param>
        /// <returns>已经激活的实例对象。</returns>
        public static GameObject Get(GameObject prefab)
        {
            if (!CheckPrefab(prefab)) return null;
            GameObjectPool pool = Create(prefab);
            GameObject item = pool.GetTyped();
#if UNITY_EDITOR
            RecordHistory($"Get GO Item | Prefab: {pool.PrefabName} | Cached: {pool.CachedCount}/{pool.MaxSize} | Active: {pool.CountActive}");
#endif
            return item;
        }

        /// <summary>
        /// 将一个 GameObject 实例归还到它原本所属的池。
        /// </summary>
        /// <param name="obj">要归还的实例对象。</param>
        /// <remarks>
        /// 这里通过实例上的 PoolRecord 找回 SourcePool，然后转交给对应的池处理。
        /// 如果对象不是从池里创建的，或者没有正确挂载 PoolRecord，就无法找到来源池。
        /// </remarks>
        public static void Return(GameObject obj)
        {
            if (!CheckPrefab(obj)) return;
            PoolRecord record = obj.GetComponent<PoolRecord>();
            if (record == null || record.SourcePool == null)
            {
                GoveKitsCore.Log(nameof(PoolCore), $"对象 {obj.name} 没有 PoolRecord 或来源池已丢失，请确认该对象是由 PoolCore.Get 创建的", logType: GoveKitsCore.LogType.Warning);
                return;
            }
            record.SourcePool.Return(obj);
#if UNITY_EDITOR
            var pool = record.SourcePool;
            RecordHistory($"Return GO Item | Prefab: {pool.PrefabName} | Cached: {pool.CachedCount}/{pool.MaxSize} | Active: {pool.CountActive}");
#endif
        }

        /// <summary>
        /// 清空某个 prefab 对应的 GameObject 对象池。
        /// </summary>
        /// <param name="prefab">用于定位池的 prefab。</param>
        public static void Clear(GameObject prefab)
        {
            if (!CheckPrefab(prefab)) return;
            int id = prefab.GetInstanceID();
            if (_gameObjectPools.TryGetValue(id, out var pool))
            {
                pool.Clear();
                _gameObjectPools.Remove(id);
#if UNITY_EDITOR
                RecordHistory($"Clear GO Pool | Prefab: {pool.PrefabName} | Id: {id}");
#endif
            }
        }


#endregion

        /// <summary>
        /// 清空所有纯 C# 对象池与 GameObject 对象池。
        /// </summary>
        public static void ClearAll()
        {
            foreach (var pool in _csharpPools.Values) pool.Clear();
            foreach (var pool in _gameObjectPools.Values) pool.Clear();
            _csharpPools.Clear();
            _gameObjectPools.Clear();
#if UNITY_EDITOR
            RecordHistory("Clear All Pools");
#endif
        }
    }
}