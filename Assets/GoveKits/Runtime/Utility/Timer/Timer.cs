using System;
using System.Collections.Generic;
using GoveKits.Pools;

namespace GoveKits.Time
{
    public class Timer : IPoolable
    {
        // --- 基础属性 ---
        public long Id { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsDone { get; private set; }
        public bool IsCancelled { get; private set; }

        // --- 内部数据 (供 TimeWheel 使用) ---
        internal Action Callback;
        internal float Interval;      // 循环间隔
        internal int LoopCount;       // 剩余循环次数 (-1 无限)
        internal long TargetTick;     // 目标触发的 Tick 时间点
        internal int Rounds;          // 剩余圈数
        internal bool UseRealTime;    // 是否受 TimeScale 影响
        
        // 关键优化：持有链表节点引用，实现 O(1) 删除/暂停
        internal LinkedListNode<Timer> LinkNode;
        // 归属的时间轮引用
        internal TimeWheel BelongsToWheel;

        // --- 暂停计算用 ---
        private float _remainingTimeOnPause; // 暂停那一刻，距离触发还剩多少秒

        public void SetID(long id) => Id = id;

        public void OnRecycle()
        {
            IsPaused = false;
            IsDone = false;
            IsCancelled = false;
            LinkNode = null;
            BelongsToWheel = null;
            Callback = null;
            _remainingTimeOnPause = 0;
        }

        // --- Public API ---

        /// <summary>
        /// 暂停 (精确暂停：冻结时间)
        /// </summary>
        public void Pause()
        {
            if (IsPaused || IsDone || IsCancelled || BelongsToWheel == null) return;

            IsPaused = true;

            // 1. 计算当前时刻距离目标触发还有多久
            // 公式：(目标Tick - 当前Tick) * Tick间隔 + 剩余圈数 * 轮盘总时长
            long currentTick = BelongsToWheel.CurrentTick;
            long tickDiff = TargetTick - currentTick; // 还有多少 Tick
            if (tickDiff < 0) tickDiff = 0;
            
            // 加上圈数的时间 (如果有的话)
            // 注意：这里简化计算，直接让 Wheel 帮我们移除，并计算剩余时间
            // 为了准确，我们在 Wheel 里处理移除逻辑
            _remainingTimeOnPause = BelongsToWheel.RemoveAndGetRemainingTime(this);
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            if (!IsPaused || IsDone || IsCancelled || BelongsToWheel == null) return;

            IsPaused = false;

            // 2. 将剩余时间重新加入时间轮
            BelongsToWheel.Reschedule(this, _remainingTimeOnPause);
        }

        /// <summary>
        /// 取消
        /// </summary>
        public void Cancel()
        {
            if (IsDone || IsCancelled) return;
            IsCancelled = true;

            // 从时间轮中移除
            if (BelongsToWheel != null)
            {
                BelongsToWheel.RemoveTimer(this);
            }
        }
    }
}