using UnityEngine;
using GoveKits;

namespace GoveKits.Pools
{
    /// <summary>
    /// 通用对象池的唯一公共入口（外观类）。
    /// 它隐藏了内部复杂的池管理逻辑，提供了简单、统一的 Get 和 Recycle 方法。
    /// 编译器会根据你传入的参数类型和泛型约束，自动选择正确的内部实现。
    /// </summary>
    public static class Pool
    {
        #region C# Object Pooling

        /// <summary>
        /// 获取一个纯 C# 对象实例。
        /// </summary>
        /// <typeparam name="T">必须是实现了 IPoolable 并有无参构造函数的 class</typeparam>
        public static T Get<T>() where T : class, IPoolable, new()
        {
            return CSharpPool<T>.Get();
        }

        /// <summary>
        /// 回收一个纯 C# 对象实例。
        /// </summary>
        public static void Recycle<T>(T obj) where T : class, IPoolable, new()
        {
            CSharpPool<T>.Recycle(obj);
        }

        #endregion

        #region Unity GameObject/Component Pooling

        /// <summary>
        /// 根据 Prefab 获取一个 Unity 组件实例。
        /// 这是推荐的获取 Unity 对象的方式，因为它兼具类型安全和便利性。
        /// </summary>
        /// <typeparam name="T">你想要获取的组件类型，例如：Bullet, EnemyController</typeparam>
        /// <param name="prefab">需要实例化的预制体 (其上必须挂载 T 组件)</param>
        /// <returns>一个已激活并重置状态的 T 组件实例</returns>
        public static T Get<T>(T prefab) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                DebugLogger.LogError("Pool", "传入的 Prefab 为空！无法创建对象。");
                return null;
            }
            // 内部调用 UnityPool，并从返回的 GameObject 上获取所需的组件。
            var instanceGo = UnityPool.Get(prefab.gameObject);
            return instanceGo.GetComponent<T>();
        }

        /// <summary>
        /// 根据 Prefab 的 GameObject 获取一个游戏对象实例。
        /// </summary>
        /// <param name="prefab">需要实例化的预制体</param>
        /// <returns>一个已激活并重置状态的游戏对象实例</returns>
        public static GameObject Get(GameObject prefab)
        {
             if (prefab == null)
            {
                DebugLogger.LogError("Pool", "传入的 Prefab 为空！无法创建对象。");
                return null;
            }
            return UnityPool.Get(prefab);
        }

        /// <summary>
        /// 回收一个 Unity 组件实例 (及其关联的 GameObject)。
        /// </summary>
        public static void Recycle(Component instance)
        {
            if (instance == null) return;
            if (!(instance is IPoolable poolable)) 
                DebugLogger.LogWarning("Pool", $"组件 '{instance.GetType().Name}' 未实现 IPoolable 接口，无法正确回收。");
            UnityPool.Recycle(instance.gameObject);
        }

        /// <summary>
        /// 回收一个游戏对象实例。
        /// </summary>
        public static void Recycle(GameObject instance)
        {
            if (instance == null) return;
            UnityPool.Recycle(instance);
        }

        #endregion
    }
}