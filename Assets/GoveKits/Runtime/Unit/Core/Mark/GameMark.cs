using System;
using UnityEngine;

namespace GoveKits.Unit
{
    /// <summary>
    /// Mark（例如 Buff/Debuff/状态标签/装备效果）的基类。
    /// <para>负责维护：标签（Tag）、来源（Source）、归属者（Owner）、层数（Stack）和持续时间（Duration）。</para>
    /// <para>设计语义：Mark 是可附加在单位上的短/长期效果单元，具有生命周期回调（OnApply/OnTick/OnStack/OnRemove），
    /// 可以被容器管理（添加、刷新、合并、移除）。</para>
    /// </summary>
    public abstract class GameMark
    {
        #region 常量定义

        /// <summary>
        /// 代表无限持续时间的值 (-1)
        /// </summary>
        public const float Infinite = -1f;

        #endregion

        #region 基础数据

        /// <summary>
        /// 标识该 Mark 的标签（高性能封装的字符串）。用于快速匹配和查询。
        /// </summary>
        public GameTag Tag { get; private set; }

        /// <summary>
        /// 此 Mark 的来源（施加者）。可为 null（表示系统/环境触发）。
        /// </summary>
        public IGameUnit Source { get; private set; }

        /// <summary>
        /// Mark 所属的单位（即被施加的目标）。在 <see cref="OnApply"/> 被调用时设置。
        /// </summary>
        public IGameUnit Owner { get; private set; }

        #endregion

        #region 堆叠与时间

        /// <summary>
        /// 最大堆叠数。<=0 表示无限堆叠。
        /// </summary>
        public int MaxStack { get; protected set; } = 1;

        /// <summary>
        /// 当前堆叠数。
        /// </summary>
        public int CurrentStack { get; protected set; } = 1;

        /// <summary>
        /// 剩余持续时间（秒）。使用 <see cref="Infinite"/> 表示永久存在。
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// 初始或最大持续时间，用于显示或刷新逻辑。-1 表示永久。
        /// </summary>
        public float MaxDuration { get; private set; }

        /// <summary>
        /// 标识该 Mark 是否已过期。
        /// <para>规则：只有当 <see cref="Duration"/> 不是永久（!= <see cref="Infinite"/>）且小于等于 0 时，视为过期。</para>
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (Duration == Infinite) return false;
                return Duration <= 0;
            }
        }

        #endregion

        public GameMark(GameTag tag, float duration = Infinite, int maxStack = 1)
        {
            Tag = tag;
            Duration = duration;
            MaxDuration = duration;
            MaxStack = maxStack;
            CurrentStack = 1;
        }

        #region 生命周期回调

        public virtual void OnApply(IGameUnit owner, IGameUnit source)
        {
            Owner = owner;
            Source = source;
        }

        public virtual void OnTick(float dt)
        {
            // 核心修改：只有非永久的 Mark 才倒计时
            if (Duration != Infinite && Duration > 0)
            {
                Duration -= dt;
            }
        }

        public virtual void OnStack(GameMark newMark)
        {
            // 1. 刷新时间逻辑
            if (newMark.Duration == Infinite)
            {
                // 如果新来的是永久的，那么我也变成永久的
                Duration = Infinite;
                MaxDuration = Infinite;
            }
            else if (Duration != Infinite)
            {
                // 如果两个都是有时限的，取最大值
                Duration = Mathf.Max(Duration, newMark.Duration);
                MaxDuration = Mathf.Max(MaxDuration, newMark.MaxDuration);
            }

            // 2. 堆叠层数
            int addStack = newMark.CurrentStack;
            if (MaxStack > 0)
                CurrentStack = Mathf.Min(CurrentStack + addStack, MaxStack);
            else
                CurrentStack += addStack;
        }

        public virtual void OnRemove()
        {
            Owner = null;
            Source = null;
        }

        #endregion
    }
}