using System;
using GoveKits.Runtime.Core.Event;

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

        public IUnit Owner { get; private set; }
        public bool IsActive { get; protected set; }

        public UnitReaction(IUnit owner)
        {
            Owner = owner;
        }

        public abstract void Activate();
        public abstract void Deactivate();

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
    public abstract class UnitReaction<T> : UnitReaction, IEventListener<T> where T : EventInfo, new()
    {
        private DisposeAction _unsubscribeAction;

        public UnitReaction(IUnit owner) : base(owner) { }

        public override void Activate()
        {
            if (IsActive) return;
            // 订阅 T 类型事件，传入 this 因为当前类实现了 IEventListener<T>
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

        // --- IEventListener<T> 接口实现 ---
        public abstract void OnEvent(T eventInfo);
        public virtual bool Filter(T eventInfo) => true; // 默认不过滤
    }


    /// <summary>
    /// 第三层：基于委托的具体反应实现。
    /// 职责：快速实例化，无需手写新类。
    /// </summary>
    public class DelegateReaction<T> : UnitReaction<T> where T : EventInfo, new()
    {
        private readonly Action<T> _reactionAction;
        
        // 字段 backing
        private readonly UnitTag _name;
        private readonly int _priority;

        public override UnitTag Name => _name;
        public override int Priority => _priority;

        public DelegateReaction(IUnit owner, UnitTag name, Action<T> reactionAction, int priority = 0) 
            : base(owner)
        {
            _name = name;
            _reactionAction = reactionAction;
            _priority = priority;
        }

        public override void OnEvent(T eventInfo)
        {
            _reactionAction?.Invoke(eventInfo);
        }
    }
}