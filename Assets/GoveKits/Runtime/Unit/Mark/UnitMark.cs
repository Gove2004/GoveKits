using System;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    public abstract class UnitMark
    {
        public abstract UnitTag Name { get; protected set; }
        public IUnit Owner { get; private set; }

        #region Data
        
        public virtual int MaxStack { get; protected set; } = 1;
        // 
        public int Stack { get; private set; } = 1;
        public float Duration { get; protected set; } = -1f; // -1 为永久
        public float Timer { get; private set; }
        // 标记是否已过期（时间到或被移除）
        public bool IsExpired { get; private set; }

        #endregion

        public UnitMark(IUnit Onwer, int stack = 1, float duration = -1f)
        {
            Owner = Onwer;
            Stack = stack;
            Duration = duration;
        }

        #region Lifecycle Methods : By the Container

        /// <summary> 首次施加时调用 </summary>
        public virtual void OnApply()
        {
            Timer = 0f;
        }

        /// <summary> 重复施加（堆叠）时调用 </summary>
        /// <param name="newMark">新传入的 Mark，用于获取它的层数或刷新时间</param>
        public virtual void OnStack(UnitMark newMark)
        {
            // 默认堆叠逻辑：增加层数并刷新剩余时间
            Stack = Math.Min(Stack + newMark.Stack, MaxStack);
            Timer = 0f;
        }

        /// <summary> 每帧执行逻辑 </summary>
        public virtual void OnUpdate(float deltaTime)
        {
            if (Duration > 0f)
            {
                Timer += deltaTime;
                if (Timer >= Duration)
                {
                    IsExpired = true;
                }
            }
        }

        /// <summary> 被强制移除时调用 </summary>
        public virtual void OnRemove() 
        {
            Owner = null;
        }

        #endregion

        #region Tools

        public float RemainingTime => Duration > 0 ? Mathf.Max(0f, Duration - Timer) : float.PositiveInfinity;

        public float Progress => Duration > 0 ? Mathf.Clamp01(Timer / Duration) : 1f;

        #endregion
    }


    /// <summary>
    /// 周期性触发逻辑的标记（如：中毒掉血、持续回蓝）
    /// </summary>
    public abstract class TickMark : UnitMark
    {
        // 改为属性或字段，不要用 abstract 并在构造函数赋值
        public float TickInterval { get; protected set; }
        private float _tickTimer;

        public TickMark(IUnit owner, float interval, int stack = 1, float duration = -1f) 
            : base(owner, stack, duration)
        {
            TickInterval = interval;
        }

        public override void OnApply()
        {
            base.OnApply();
            _tickTimer = 0f;
            
            // 需求决定：是否在施加瞬间立即触发一次？
            // OnTick(); 
        }

        public override void OnUpdate(float deltaTime)
        {
            // 必须调用 base.OnUpdate 确保持续时间（Duration）逻辑正常运行
            base.OnUpdate(deltaTime);

            if (IsExpired) return;

            if (TickInterval > 0f)
            {
                _tickTimer += deltaTime;
                if (_tickTimer >= TickInterval)
                {
                    // 使用减法防止帧率波动导致的计时丢失
                    _tickTimer -= TickInterval;
                    OnTick();
                }
            }
        }

        /// <summary>
        /// 周期性触发的逻辑入口
        /// </summary>
        protected abstract void OnTick();
    }
}