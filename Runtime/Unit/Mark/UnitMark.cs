using System;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 标记基类，用于表示单位的各种状态效果（如Buff/Debuff/持续效果等）
    /// </summary>
    public abstract class UnitMark
    {
        /// <summary>
        /// 标记的唯一标识名称
        /// </summary>
        public abstract UnitTag Name { get; protected set; }
        
        /// <summary>
        /// 标记所属的单位
        /// </summary>
        public IUnit Owner { get; protected set; }

        #region 数据属性
        
        /// <summary>
        /// 最大堆叠层数，默认为1
        /// </summary>
        public virtual int MaxStack { get; protected set; } = 1;
        
        /// <summary>
        /// 当前堆叠层数，默认为1
        /// </summary>
        public int Stack { get; private set; } = 1;
        
        /// <summary>
        /// 持续时间（秒），-1表示永久持续
        /// </summary>
        public float Duration { get; protected set; } = -1f; // -1 为永久
        
        /// <summary>
        /// 计时器，记录已持续的时间
        /// </summary>
        public float Timer { get; private set; }
        
        /// <summary>
        /// 标记是否已过期（时间到或被移除）
        /// </summary>
        public bool IsExpired { get; private set; }

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">标记所属单位</param>
        /// <param name="stack">堆叠层数，默认为1</param>
        /// <param name="duration">持续时间（秒），-1表示永久，默认为-1</param>
        public UnitMark(IUnit owner, int stack = 1, float duration = -1f)
        {
            Owner = owner;
            Stack = stack;
            Duration = duration;
        }

        #region 生命周期方法：由容器调用

        /// <summary>
        /// 标记首次施加时调用
        /// </summary>
        public virtual void OnApply()
        {
            Timer = 0f;
        }

        /// <summary>
        /// 标记重复施加（堆叠）时调用
        /// </summary>
        /// <param name="newMark">新传入的标记实例，用于获取它的层数或刷新时间</param>
        public virtual void OnStack(UnitMark newMark)
        {
            // 默认堆叠逻辑：增加层数并刷新剩余时间
            Stack = Math.Min(Stack + newMark.Stack, MaxStack);
            Timer = 0f;
        }

        /// <summary>
        /// 每帧更新逻辑
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
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

        /// <summary>
        /// 标记被强制移除时调用
        /// </summary>
        public virtual void OnRemove() 
        {
            Owner = null;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 剩余时间，如果持续时间为永久则返回正无穷
        /// </summary>
        public float RemainingTime => Duration > 0 ? Mathf.Max(0f, Duration - Timer) : float.PositiveInfinity;

        /// <summary>
        /// 完成进度，范围在0到1之间，如果持续时间为永久则返回1
        /// </summary>
        public float Progress => Duration > 0 ? Mathf.Clamp01(Timer / Duration) : 1f;

        #endregion
    }


    /// <summary>
    /// 周期性触发逻辑的标记（如：中毒掉血、持续回蓝）
    /// </summary>
    public abstract class TickMark : UnitMark
    {
        /// <summary>
        /// 周期间隔时间
        /// </summary>
        public float TickInterval { get; protected set; }
        
        /// <summary>
        /// 内部计时器，用于跟踪到下次触发的时间
        /// </summary>
        private float _tickTimer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">标记所属单位</param>
        /// <param name="interval">触发间隔时间</param>
        /// <param name="stack">堆叠层数，默认为1</param>
        /// <param name="duration">持续时间（秒），-1表示永久，默认为-1</param>
        public TickMark(IUnit owner, float interval, int stack = 1, float duration = -1f) 
            : base(owner, stack, duration)
        {
            TickInterval = interval;
        }

        /// <summary>
        /// 标记应用时的初始化逻辑
        /// </summary>
        public override void OnApply()
        {
            base.OnApply();
            _tickTimer = 0f;
            
            // 需求决定：是否在施加瞬间立即触发一次？
            // OnTick(); 
        }

        /// <summary>
        /// 更新标记逻辑，包括持续时间和周期性触发
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public override void OnUpdate(float deltaTime)
        {
            // 必须调用 base.OnUpdate 确保持续时间（Duration）逻辑正常运行
            base.OnUpdate(deltaTime);

            if (IsExpired) return;

            if (TickInterval > 0f)
            {
                _tickTimer += deltaTime;
                // 检查是否到达下一个tick时间点
                while (_tickTimer >= TickInterval) {
                    _tickTimer -= TickInterval;
                    OnTick();
                    if (IsExpired) break; // 如果某次 Tick 导致了过期，立刻终止
                }
            }
        }

        /// <summary>
        /// 周期性触发的逻辑入口，子类必须实现具体的周期行为
        /// </summary>
        protected abstract void OnTick();
    }
}