

using System;
using System.Collections.Generic;
using GoveKits.Manager;
using UnityEngine;

namespace GoveKits.Pool
{
    /// <summary>
    /// 对象池管理器（单例）
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>
    {
        // 对象池字典：键为Prefab类型名，值为对应的对象池
        private readonly Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();
        
        /// <summary>
        /// 初始化对象池, 建议在游戏启动时预初始化常用对象池
        /// </summary>
        /// <param name="prefab">Prefab模板</param>
        /// <param name="initialSize">初始池大小</param>
        public void InitializePool(string className, GameObject prefab, int initialSize = 8)
        {
            if (!_pools.ContainsKey(className))
            {
                _pools[className] = new Pool(prefab, initialSize, transform);
            }
            else
            {
                Debug.LogWarning($"[PoolManager] Pool for {className} already exists.");
            }
        }
        
        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        /// <param name="prefab">Prefab模板</param>
        /// <returns>可用的游戏对象</returns>
        public GameObject GetObject(string className)
        {
            return _pools[className].GetObject();
        }
        
        /// <summary>
        /// 将对象返回对象池
        /// </summary>
        /// <param name="obj">要返回的游戏对象</param>
        public void ReturnObject(string className, GameObject obj)
        {
            if (_pools.ContainsKey(className))
            {
                _pools[className].ReturnObject(obj);
            }
            else
            {
                // 如果没有找到对应的池，直接销毁
                Destroy(obj);
            }
        }

        /// <summary>
        /// 获取指定类型的池
        /// </summary>
        public Pool GetPool(string className)
        {
            return _pools.ContainsKey(className) ? _pools[className] : null;
        }
        
        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }
    }
}