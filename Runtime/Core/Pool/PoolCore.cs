using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 对象池系统总入口
    /// </summary>
    /// <remarks>
    /// 这个类负责维护所有类型对应的池实例，并对外提供统一的 Create / Get / Return / Clear 接口
    /// 
    /// 当前实现分成两条线：
    /// 1. 纯 C# 对象池：按 Type 存放在 _csharpPools
    /// 2. GameObject 池：按 prefab 的 InstanceID 存放在 _gameObjectPools
    /// 
    /// 采用静态类设计，全局可访问，无需实例化
    /// </remarks>
    public static class PoolCore
    {
        // ==================== 默认配置常量 ====================
        
        /// <summary>
        /// 默认 C# 对象池预热数量
        /// </summary>
        private const int DefaultCSharpPoolCount = 0;
        
        /// <summary>
        /// 默认 C# 对象池最大容量
        /// </summary>
        private const int DefaultCSharpPoolMaxSize = 16;
        
        /// <summary>
        /// 默认 GameObject 池预热数量
        /// </summary>
        private const int DefaultGameObjectPoolCount = 0;
        
        /// <summary>
        /// 默认 GameObject 池最大容量
        /// </summary>
        private const int DefaultGameObjectPoolMaxSize = 16;

        // ==================== 池存储容器 ====================

        /// <summary>
        /// C# 对象池字典
        /// 键：对象类型 Type，值：IPool 接口（多态存储）
        /// </summary>
        private static readonly Dictionary<Type, IPool> _csharpPools = new();
        
        /// <summary>
        /// GameObject 对象池字典
        /// 键：预制体 InstanceID，值：GameObjectPool
        /// 使用 InstanceID 确保同一预制体只对应一个池
        /// </summary>
        private static readonly Dictionary<int, GameObjectPool> _gameObjectPools = new();

        #region C# 对象池管理

        /// <summary>
        /// 创建或获取一个纯 C# 对象池
        /// </summary>
        /// <typeparam name="T">池化类型</typeparam>
        /// <param name="count">首次创建时的预热数量</param>
        /// <param name="maxSize">池最大缓存数量</param>
        /// <returns>类型 T 对应的对象池</returns>
        /// <remarks>
        /// 如果该类型的池已经存在，则直接返回已有池
        /// 不会重复创建，也不会重新应用新的 count / maxSize 参数
        /// </remarks>
        public static CSharpPool<T> Create<T>(int count = DefaultCSharpPoolCount, int maxSize = DefaultCSharpPoolMaxSize) 
            where T : class, IPoolable, new()
        {
            var type = typeof(T);
            
            // 尝试获取已存在的池
            if (!_csharpPools.TryGetValue(type, out var pool))
            {
                // 池不存在，创建新池
                pool = new CSharpPool<T>(maxSize);
                // 执行预热
                pool.Warmup(count);
                // 注册到字典
                _csharpPools[type] = pool;
            }
            
            return (CSharpPool<T>)pool;
        }

        /// <summary>
        /// 从纯 C# 对象池中取出一个对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>池中已有对象，或在池为空时新创建的对象</returns>
        public static T Get<T>() where T : class, IPoolable, new()
        {
            // 自动创建池并获取对象
            return Create<T>().Get();
        }

        /// <summary>
        /// 归还一个纯 C# 对象到对应类型的池中
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="item">要归还的对象实例</param>
        public static void Return<T>(T item) where T : class, IPoolable, new()
        {
            if (item == null) return;
            Create<T>().Return(item);
        }

        /// <summary>
        /// 清空指定类型的纯 C# 对象池
        /// </summary>
        /// <typeparam name="T">要清理的对象类型</typeparam>
        public static void Clear<T>() where T : class, IPoolable, new()
        {
            var type = typeof(T);
            if (_csharpPools.TryGetValue(type, out var pool))
            {
                pool.Clear();
                // 从字典中移除
                _csharpPools.Remove(type);
            }
        }

        #endregion

        #region GameObject 对象池管理

        /// <summary>
        /// 创建或获取一个 GameObject 对象池
        /// </summary>
        /// <param name="prefab">作为池模板的预制体</param>
        /// <param name="count">首次创建时的默认容量</param>
        /// <param name="maxSize">池最大缓存数量</param>
        /// <returns>该预制体对应的 GameObjectPool</returns>
        /// <remarks>
        /// 池按预制体的 InstanceID 做唯一索引
        /// 已创建过的预制体再次调用 Create 时，不会重新创建池，也不会更新参数
        /// </remarks>
        public static GameObjectPool Create(GameObject prefab, int count = DefaultGameObjectPoolCount, int maxSize = DefaultGameObjectPoolMaxSize)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            
            // 使用 InstanceID 作为唯一标识
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
        /// 从指定预制体对应的池中取出一个实例
        /// </summary>
        /// <param name="prefab">池模板预制体</param>
        /// <returns>已经激活的实例对象</returns>
        public static GameObject Get(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return Create(prefab).Get();
        }

        /// <summary>
        /// 将一个 GameObject 实例归还到它原本所属的池
        /// </summary>
        /// <param name="obj">要归还的实例对象</param>
        /// <remarks>
        /// 这里通过实例上的 PoolRecord 找回 SourcePool，然后转交给对应的池处理
        /// 如果对象不是从池里创建的，或者没有正确挂载 PoolRecord，就无法找到来源池
        /// 此时会直接销毁对象，避免内存泄漏
        /// </remarks>
        public static void Return(GameObject obj)
        {
            if (obj == null) return;
            
            // 获取池记录组件
            PoolRecord record = obj.GetComponent<PoolRecord>();
            
            // 找不到来源池，直接销毁
            if (record == null || record.SourcePool == null)
            {
                GameObject.Destroy(obj);
                return;
            }
            
            // 归还到来源池
            record.SourcePool.Return(obj);
        }

        /// <summary>
        /// 清空某个预制体对应的 GameObject 对象池
        /// </summary>
        /// <param name="prefab">用于定位池的预制体</param>
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
        /// 清空所有纯 C# 对象池与 GameObject 对象池
        /// </summary>
        /// <remarks>
        /// 通常在场景切换或游戏结束时调用
        /// 释放所有池化资源，避免内存泄漏
        /// </remarks>
        public static void ClearAll()
        {
            foreach (var pool in _csharpPools.Values) pool.Clear();
            foreach (var pool in _gameObjectPools.Values) pool.Clear();
            _csharpPools.Clear();
            _gameObjectPools.Clear();
        }
    }
}