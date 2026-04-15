using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 第一层：Unit 反应基类（非泛型公共抽象层）。
    /// 职责：仅提供生命周期管理、统一状态存储，不负责具体的事件监听类型。
    /// </summary>
    public abstract class UnitReaction : IDisposable
    {
        /// <summary>
        /// 反应的唯一标识（通常与监听的事件或业务逻辑相关）。
        /// </summary>
        public abstract UnitTag Name { get; }
        
        /// <summary>
        /// 决定在事件总线（EventBus）中被调用的先后顺序。
        /// 值越大，优先级越高（越先执行）。
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// 反应挂载的宿主单位（由 Container 注入）。
        /// </summary>
        public IUnit Owner { get; private set; }
        
        /// <summary>
        /// 反应是否处于激活监听状态。
        /// </summary>
        public bool IsActive { get; protected set; }

        /// <summary>无参构造，满足反序列化工厂要求</summary>
        public UnitReaction() { }

        /// <summary>
        /// 由 ReactionContainer 在挂载瞬间调用，注入灵魂。
        /// </summary>
        internal void Init(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>激活反应，开始向全局 EventBus 注册监听事件</summary>
        public abstract void Activate();
        
        /// <summary>停用反应，从全局 EventBus 注销监听事件</summary>
        public abstract void Deactivate();

        /// <summary>释放反应资源，彻底断开与宿主及事件总线的联系</summary>
        public virtual void Dispose()
        {
            Deactivate();
            Owner = null;
        }
    }


    /// <summary>
    /// 第二层：泛型 Unit 反应基类。
    /// 职责：实现 <see cref="IEventListener{T}"/> 接口，严格管理具体事件类型的订阅与反订阅生命周期。
    /// </summary>
    public abstract class UnitReaction<T> : UnitReaction, IEventListener<T> where T : EventData, new()
    {
        // 保存订阅成功后返回的注销凭证，防止内存泄漏
        private IDisposable _unsubscribeAction;

        public UnitReaction() { }

        public override void Activate()
        {
            if (IsActive) return;
            
            // 订阅 T 类型事件，传入 this，因为当前类实现了 IEventListener<T>
            _unsubscribeAction = EventCore.Subscribe<T>(this);
            IsActive = true;
        }

        public override void Deactivate()
        {
            if (!IsActive) return;
            
            _unsubscribeAction?.Dispose();
            _unsubscribeAction = null;
            IsActive = false;
        }

        // ================= IEventListener<T> 接口实现 =================
        
        /// <summary>
        /// 事件处理核心业务逻辑，由 EventBus 触发。
        /// </summary>
        /// <param name="eventInfo">包含受击、治疗、移动等信息的事件数据包。</param>
        public abstract void OnEvent(T eventInfo);
        
        /// <summary>
        /// 事件前置过滤网关。
        /// <para>可以在此处判断：这个受击事件的目标是不是我的 Owner？如果不是则返回 false 忽略该事件。</para>
        /// </summary>
        public virtual bool OnFilter(T eventInfo) => true; 
    }
}