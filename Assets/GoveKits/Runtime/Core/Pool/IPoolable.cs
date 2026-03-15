using UnityEngine;

namespace GoveKits.Runtime.Core.Pool
{
    /// <summary>
    /// 可被池系统管理的对象接口。
    /// </summary>
    /// <remarks>
    /// 这套接口同时服务于两类对象：
    /// 1. 纯 C# 对象：例如临时数据、配置快照、战斗结算对象。
    /// 2. GameObject 组件：挂在场景对象或 prefab 上，由 GameObjectPool 管理。
    /// 
    /// 一个生命周期回调：<see cref="OnRecycle"/>。
    /// 它在对象被归还到池中时调用，用于统一做清理和状态重置。
    /// </remarks>
    public interface IPoolable
    {
        /// <summary>
        /// 当对象被归还到池中时调用。
        /// </summary>
        /// <remarks>
        /// 建议在这里做以下事情：
        /// - 清空外部引用
        /// - 重置运行时字段
        /// - 停止协程、Tween、特效或计时器
        /// - 恢复到可再次复用的干净状态
        /// </remarks>
        void OnRecycle();
    }

    /// <summary>
    /// 池化对象的便捷扩展方法。
    /// </summary>
    /// <remarks>
    /// 作用只是把调用写法从 PoolCore.Return(x) 简化为 x.ReturnToPool()。
    /// 这样业务代码可读性更高，也更接近“对象自己回池”的语义。
    /// </remarks>
    public static class PoolableExtensions
    {
        /// <summary>
        /// 归还一个纯 C# 池对象。
        /// </summary>
        /// <typeparam name="T">对象类型，必须实现 <see cref="IPoolable"/> 且带无参构造。</typeparam>
        /// <param name="item">要归还的对象实例。</param>
        public static void ReturnToPool<T>(this T item) where T : class, IPoolable, new()
            => PoolCore.Return(item);

        /// <summary>
        /// 归还一个 GameObject 池对象。
        /// </summary>
        /// <param name="item">要归还的场景对象。</param>
        public static void ReturnToPool(this GameObject item)
            => PoolCore.Return(item);
    }
}