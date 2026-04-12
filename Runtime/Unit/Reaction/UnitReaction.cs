using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 第一层：Unit 反应基类（非泛型公共层）。
    /// 职责：仅提供生命周期管理、统一存储和基础属性，【不负责】事件的监听。
    /// </summary>
    public abstract class UnitReaction : IDisposable
    {
        /// <summary>
        /// 反应唯一标识。
        /// </summary>
        public abstract UnitTag Name { get; }
        
        /// <summary>
        /// 统一的优先级接口（改为 abstract，强制子类提供）
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// 反应所属的单位
        /// </summary>
        public IUnit Owner { get; private set; }
        
        /// <summary>
        /// 反应是否处于激活状态
        /// </summary>
        public bool IsActive { get; protected set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">反应所属的单位</param>
        public UnitReaction(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 激活反应，开始监听事件
        /// </summary>
        public abstract void Activate();
        
        /// <summary>
        /// 停用反应，停止监听事件
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// 释放反应资源
        /// </summary>
        public virtual void Dispose()
        {
            Deactivate();
            Owner = null;
        }
    }


    /// <summary>
    /// 第二层：泛型 Unit 反应基类。
    /// 职责：实现 IEventListener<T> 接口，管理具体事件类型的订阅与反订阅。
    /// </summary>
    public abstract class UnitReaction<T> : UnitReaction, IEventListener<T> where T : EventData, new()
    {
        /// <summary>
        /// 取消订阅动作，用于在停用时取消事件订阅
        /// </summary>
        private IDisposable _unsubscribeAction;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">反应所属的单位</param>
        public UnitReaction(IUnit owner) : base(owner) { }

        /// <summary>
        /// 激活反应，订阅指定类型事件
        /// </summary>
        public override void Activate()
        {
            if (IsActive) return;
            // 订阅 T 类型事件，传入 this 因为当前类实现了 IEventListener<T>
            _unsubscribeAction = EventCore.Subscribe<T>(this);
            IsActive = true;
        }

        /// <summary>
        /// 停用反应，取消事件订阅
        /// </summary>
        public override void Deactivate()
        {
            if (!IsActive) return;
            _unsubscribeAction?.Dispose();
            _unsubscribeAction = null;
            IsActive = false;
        }

        // --- IEventListener<T> 接口实现 ---
        
        /// <summary>
        /// 处理事件的回调方法，由事件系统调用
        /// </summary>
        /// <param name="eventInfo">事件数据</param>
        public abstract void OnEvent(T eventInfo);
        
        /// <summary>
        /// 事件过滤方法，决定是否处理该事件
        /// </summary>
        /// <param name="eventInfo">事件数据</param>
        /// <returns>true表示处理事件，false表示忽略事件</returns>
        public virtual bool OnFilter(T eventInfo) => true; // 默认不过滤
    }
}