using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 可被池系统管理的对象接口
    /// </summary>
    /// <remarks>
    /// 这套接口同时服务于两类对象：
    /// 1. 纯 C# 对象：例如临时数据、配置快照、战斗结算对象
    /// 2. GameObject 组件：挂在场景对象或 prefab 上，由 GameObjectPool 管理
    /// 
    /// 一个生命周期回调：OnRecycle
    /// 它在对象被归还到池中时调用，用于统一做清理和状态重置
    /// </remarks>
    public interface IPoolable
    {
        /// <summary>
        /// 当对象被归还到池中时调用
        /// </summary>
        /// <remarks>
        /// 建议在这里做以下事情：
        /// - 清空外部引用，避免内存泄漏
        /// - 重置运行时字段，恢复初始状态
        /// - 停止协程、Tween、特效或计时器
        /// - 恢复到可再次复用的干净状态
        /// </remarks>
        void OnRecycle();
    }
}