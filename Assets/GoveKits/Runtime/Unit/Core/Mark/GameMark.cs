using System;
using UnityEngine;
using GoveKits.Times;

namespace GoveKits.Unit
{
    /// <summary>
    /// 状态标记基类
    /// <para>支持自动销毁 (Duration) 和 周期性心跳 (Interval)。</para>
    /// </summary>
    public abstract class GameMark
    {
        #region Configuration (配置)

        public GameTag Tag { get; private set; }
        public float Duration { get; protected set; }
        public int MaxStack { get; private set; }

        public const float Infinite = -1f;

        /// <summary>
        /// 心跳间隔 (秒)
        /// <para>重写此属性返回 > 0 的值以启用 OnTick</para>
        /// <para>例如：返回 1.0f 表示每秒触发一次 OnTick</para>
        /// </summary>
        protected virtual float TickInterval => -1f;

        #endregion

        #region Runtime State (运行时状态)

        public IGameUnit Owner { get; private set; }
        public IGameUnit Source { get; private set; }
        public int CurrentStack { get; protected set; } = 1;

        private float _startTime;
        private Timer _expireTimer; // 负责销毁
        private Timer _tickTimer;   // 负责心跳

        #endregion

        protected GameMark(GameTag tag, float duration = Infinite, int maxStack = 1)
        {
            Tag = tag;
            Duration = duration;
            MaxStack = maxStack;
        }

        #region Lifecycle (生命周期)

        public virtual void OnApply(IGameUnit owner, IGameUnit source)
        {
            Owner = owner;
            Source = source;
            
            // 1. 启动销毁倒计时
            RefreshExpireTimer(Duration);

            // 2. 启动周期性心跳
            RefreshTickTimer();
        }

        public virtual void OnStack(GameMark newMark)
        {
            // 合并层数
            if (MaxStack > 0)
                CurrentStack = Math.Min(CurrentStack + newMark.CurrentStack, MaxStack);
            else
                CurrentStack += newMark.CurrentStack;

            // 刷新时间
            float newDuration = (newMark.Duration == Infinite) 
                ? Infinite 
                : Math.Max(Duration, newMark.Duration);

            RefreshExpireTimer(newDuration);
            
            // 堆叠通常不重置心跳计时器，保持原节奏
            // 如果需要重置心跳，可以在这里调用 RefreshTickTimer()
            // RefreshTickTimer();
        }

        public virtual void OnRemove()
        {
            StopExpireTimer();
            StopTickTimer();
            
            Owner = null;
            Source = null;
        }

        #endregion

        #region Loop Logic (心跳逻辑)

        /// <summary>
        /// 周期性逻辑回调 (替代 Update)
        /// <para>只有当 TickInterval > 0 时会被调用</para>
        /// </summary>
        protected virtual void OnTick() { }

        private void RefreshTickTimer()
        {
            StopTickTimer();

            float interval = TickInterval;
            if (interval > 0)
            {
                // 使用 TimerManager 创建循环定时器
                // 无限循环，直到 Mark 被移除时手动 Cancel
                _tickTimer = TimerManager.Loop(interval, OnTick, -1);
            }
        }

        private void StopTickTimer()
        {
            if (_tickTimer != null)
            {
                _tickTimer.Cancel();
                _tickTimer = null;
            }
        }

        #endregion

        #region Expire Logic (销毁逻辑)

        private void RefreshExpireTimer(float duration)
        {
            StopExpireTimer();

            Duration = duration;
            _startTime = UnityEngine.Time.time;

            if (duration != Infinite && duration > 0)
            {
                _expireTimer = TimerManager.Once(duration, OnExpire);
            }
        }

        private void StopExpireTimer()
        {
            if (_expireTimer != null)
            {
                _expireTimer.Cancel();
                _expireTimer = null;
            }
        }

        private void OnExpire()
        {
            Owner?.Marks?.Remove(Tag);
        }

        #endregion

        #region Helpers

        public float RemainingTime
        {
            get
            {
                if (Duration == Infinite) return float.MaxValue;
                if (_expireTimer == null) return 0f;
                return Mathf.Max(0f, Duration - ( UnityEngine.Time.time - _startTime));
            }
        }

        public float Progress
        {
            get
            {
                if (Duration == Infinite || Duration <= 0) return 1f;
                return Mathf.Clamp01(RemainingTime / Duration);
            }
        }

        #endregion
    }
}