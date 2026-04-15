using System;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 标记基类，用于表示单位附着的各种状态效果（如 Buff / Debuff / 护盾 / 标记层数）。
    /// </summary>
    public abstract class UnitMark
    {
        /// <summary>标记的唯一标识名称</summary>
        public abstract UnitTag Name { get; protected set; }
        
        /// <summary>标记挂载的宿主单位（由 Container 注入）</summary>
        public IUnit Owner { get; protected set; }

        #region 核心状态数据
        
        /// <summary>最大可叠加层数（默认为1，不可叠加）</summary>
        public virtual int MaxStack { get; protected set; } = 1;
        
        /// <summary>当前已叠加层数</summary>
        public int Stack { get; private set; } = 1;
        
        /// <summary>状态持续时间（秒）。-1 表示永久持续，直到被手动移除。</summary>
        public float Duration { get; protected set; } = -1f; 
        
        /// <summary>状态流失时间计时器</summary>
        public float Timer { get; private set; }
        
        /// <summary>当前标记是否已经完成生命周期（可被安全移除）</summary>
        public bool IsExpired { get; private set; }

        /// <summary>剩余时间，如果持续时间为永久则返回正无穷</summary>
        public float RemainingTime => Duration > 0 ? Mathf.Max(0f, Duration - Timer) : float.PositiveInfinity;

        /// <summary>完成进度，范围在 0~1 之间。如果是永久 Buff 则恒返回 1f</summary>
        public float Progress => Duration > 0 ? Mathf.Clamp01(Timer / Duration) : 1f;

        #endregion

        /// <summary>无参构造，满足反序列化工厂要求</summary>
        public UnitMark() { }

        // ================== 注入装配接口 (供 Factory 与 Container 使用) ==================

        /// <summary>用于配置化的链式数据装配</summary>
        internal UnitMark SetData(int stack, float duration)
        {
            Stack = stack;
            Duration = duration;
            return this;
        }

        internal UnitMark SetStack(int stack) { Stack = stack; return this; }
        internal UnitMark SetDuration(float duration) { Duration = duration; return this; }

        /// <summary>由 MarkContainer 在挂载瞬间调用，注入灵魂</summary>
        internal void Init(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>专门提供给序列化模块使用，用于读档时精准恢复 Buff 进度</summary>
        internal void RestoreTimer(float timer) => Timer = timer;

        #region 生命周期回调 (由容器驱动)

        /// <summary>标记被首次挂载到身上时触发</summary>
        public virtual void OnApply()
        {
            Timer = 0f;
        }

        /// <summary>
        /// 宿主身上已存在同名标记时触发（处理堆叠冲突逻辑）。
        /// 默认行为：层数合并（不超过上限），并刷新剩余持续时间。
        /// </summary>
        public virtual void OnStack(UnitMark newMark)
        {
            Stack = Math.Min(Stack + newMark.Stack, MaxStack);
            Timer = 0f;
        }

        /// <summary>每帧更新逻辑，处理时间流逝。</summary>
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

        /// <summary>标记时间到期，或被驱散时触发，执行扫尾逻辑。</summary>
        public virtual void OnRemove() 
        {
            Owner = null;
        }

        #endregion
    }

    /// <summary>
    /// 周期性触发的特殊标记（如：中毒掉血、缓慢回蓝、燃烧）。
    /// </summary>
    public abstract class TickMark : UnitMark
    {
        /// <summary>两次触发之间的间隔时间</summary>
        public float TickInterval { get; protected set; }
        
        private float _tickTimer;

        public TickMark() { }

        /// <summary>设置触发频率</summary>
        public TickMark SetInterval(float interval)
        {
            TickInterval = interval;
            return this;
        }

        public override void OnApply()
        {
            base.OnApply();
            _tickTimer = 0f;
            // 默认设计为施加瞬间不立即触发，如果有首跳伤害需求，子类可在此调用 OnTick()
        }

        public override void OnUpdate(float deltaTime)
        {
            // 必须调用 base.OnUpdate 让总持续时间（Duration）逻辑正常运作
            base.OnUpdate(deltaTime);

            if (IsExpired) return;

            if (TickInterval > 0f)
            {
                _tickTimer += deltaTime;
                
                // 追赶机制：处理极低帧率下单帧跨过多周期的跳字补偿
                while (_tickTimer >= TickInterval) 
                {
                    _tickTimer -= TickInterval;
                    OnTick();
                    
                    // 若某次 Tick 触发的逻辑导致该标记提前死亡（比如触发解毒），则立即终止迭代
                    if (IsExpired) break; 
                }
            }
        }

        /// <summary>
        /// 周期性触发的业务逻辑入口。
        /// 子类可在此处编写：创建 AttributeChangeEffect 扣减宿主的生命值。
        /// </summary>
        protected abstract void OnTick();
    }
}