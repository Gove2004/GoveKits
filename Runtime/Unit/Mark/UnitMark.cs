using System;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 标记基类，用于表示单位附着的各种状态效果（如 Buff / Debuff / 护盾 / 标记层数）。
    /// Mark 自身只存数据，不处理业务逻辑；周期的业务行为在 TickMark.OnTick 中产出 Effect 或发布事件。
    /// </summary>
    public abstract class UnitMark
    {
        /// <summary>标记的唯一标识名称</summary>
        public abstract UnitTag Name { get; protected set; }

        /// <summary>标记挂载的宿主单位（由 Container 注入）</summary>
        public IUnit Owner { get; protected set; }

        #region 核心状态数据

        /// <summary>最大可叠加层数（默认为 1，不可叠加）</summary>
        public virtual int MaxStack { get; protected set; } = 1;

        /// <summary>当前已叠加层数</summary>
        public int Stack { get; private set; } = 1;

        /// <summary>状态持续时间（秒）。-1 表示永久持续，直到被手动移除</summary>
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

        /// <summary>内部方法：设置层数和持续时间</summary>
        internal UnitMark SetData(int stack, float duration)
        {
            Stack = stack;
            Duration = duration;
            return this;
        }

        /// <summary>内部方法：设置层数</summary>
        internal UnitMark SetStack(int stack) { Stack = stack; return this; }

        /// <summary>内部方法：设置持续时间</summary>
        internal UnitMark SetDuration(float duration) { Duration = duration; return this; }

        /// <summary>由 MarkContainer 在挂载瞬间调用，注入宿主引用</summary>
        internal void Init(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>专门提供给序列化模块使用，用于读档时精准恢复 Buff 进度</summary>
        internal void RestoreTimer(float timer) => Timer = timer;

        #region 生命周期回调 (由容器驱动)

        /// <summary>标记被首次挂载到身上时触发。重置计时并清除过期状态，保证复用实例状态干净</summary>
        public virtual void OnApply()
        {
            Timer = 0f;
            IsExpired = false;
        }

        /// <summary>
        /// 宿主身上已存在同名标记时触发（处理堆叠冲突逻辑）。
        /// 默认行为：层数合并（不超过上限），并刷新剩余持续时间；同时清除过期状态（叠层可复活过期标记）。
        /// </summary>
        public virtual void OnStack(UnitMark newMark)
        {
            Stack = Math.Min(Stack + newMark.Stack, MaxStack);
            Timer = 0f;
            IsExpired = false;
        }

        /// <summary>每帧更新逻辑，处理时间流逝</summary>
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

        /// <summary>标记时间到期，或被驱散时触发，执行扫尾逻辑</summary>
        public virtual void OnRemove()
        {
            Owner = null;
        }

        /// <summary>驱散路径的过期标记（UpdateMarks 快照以此跳过已移除实例，防止幽灵 tick）。</summary>
        internal void MarkRemoved() => IsExpired = true;

        #endregion
    }

    /// <summary>
    /// 周期性触发的特殊标记（如：中毒掉血、缓慢回蓝、燃烧）。
    /// 计时与触发节奏由基类统一管理，业务逻辑写在 <see cref="OnTick"/> 中。
    /// </summary>
    public abstract class TickMark : UnitMark
    {
        /// <summary>两次触发之间的间隔时间</summary>
        public float TickInterval { get; protected set; }

        private float _tickTimer;

        /// <summary>无参构造</summary>
        public TickMark() { }

        /// <summary>设置触发频率</summary>
        public TickMark SetInterval(float interval)
        {
            TickInterval = interval;
            return this;
        }

        /// <summary>周期计时器当前值（供序列化模块读取运行态进度）</summary>
        internal float TickTimer => _tickTimer;

        /// <summary>供序列化模块读档恢复周期进度（须在 AddMark 之后调用，OnApply 会清零）</summary>
        internal void RestoreTickTimer(float timer) => _tickTimer = timer;

        /// <summary>
        /// 标记被首次挂载时触发，额外初始化周期计时器。
        /// </summary>
        public override void OnApply()
        {
            base.OnApply();
            _tickTimer = 0f;
        }

        /// <summary>
        /// 每帧更新逻辑，额外处理周期性触发。
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            if (IsExpired) return;

            if (TickInterval > 0f)
            {
                _tickTimer += deltaTime;
                while (_tickTimer >= TickInterval)
                {
                    _tickTimer -= TickInterval;
                    OnTick();
                    if (IsExpired) break;
                }
            }
        }

        /// <summary>周期性触发的业务逻辑入口，子类在此产出 Effect（如 AttributeChangeEffect）或发布事件</summary>
        protected abstract void OnTick();
    }
}
