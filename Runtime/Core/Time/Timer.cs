
using System;
using System.Collections.Generic;


namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 轻量定时器对象：状态数据载体，无回收逻辑（由 TimeWheel 统一管理）。
    /// </summary>
    public class Timer : IPoolable
    {
        // --- 状态标识 ---
        public long Id { get; private set; }
        public bool IsPaused { get; internal set; }
        public bool IsDone { get; internal set; }
        public bool IsCancelled { get; internal set; }

        // --- 调度数据 ---
        internal Action Callback;
        internal float Interval;
        internal int LoopCount;       // 剩余循环次数 (-1 无限)
        internal long TargetTick;
        internal int Rounds;
        
        // --- 链表节点（O(1) 操作关键）---
        internal LinkedListNode<Timer> LinkNode;
        internal TimeWheel BelongsToWheel;

        // --- 暂停数据 ---
        internal float RemainingTimeOnPause;

        public void SetID(long id) => Id = id;

        /// <summary>
        /// 重置所有状态（对象池回收时调用）。
        /// </summary>
        public void OnRecycle()
        {
            Id = 0;
            IsPaused = false;
            IsDone = false;
            IsCancelled = false;
            Callback = null;
            Interval = 0;
            LoopCount = 0;
            TargetTick = 0;
            Rounds = 0;
            RemainingTimeOnPause = 0;
            LinkNode = null;
            BelongsToWheel = null;
        }

        // --- Public API：只标记状态，不直接操作 ---

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            if (IsPaused || IsDone || IsCancelled || BelongsToWheel == null) return;
            IsPaused = true;
            // 计算剩余时间，从时间轮移除（但不回收）
            RemainingTimeOnPause = BelongsToWheel.RemoveAndCalcRemaining(this);
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            if (!IsPaused || IsDone || IsCancelled || BelongsToWheel == null) return;
            IsPaused = false;
            BelongsToWheel.Schedule(this, RemainingTimeOnPause);
        }

        /// <summary>
        /// 取消
        /// </summary>
        public void Cancel()
        {
            if (IsDone || IsCancelled) return;
            IsCancelled = true;
            // 标记为待移除，由 Wheel 在 Process 时统一处理并回收
            // 如果正在当前槽位处理中，立即处理
            if (BelongsToWheel != null && BelongsToWheel.IsProcessing)
            {
                // Wheel 会在本轮处理完后回收
                return;
            }
            // 否则立即从链表移除，等待 Wheel 的下一轮处理或立即回收
            BelongsToWheel?.MarkForRemove(this);
        }
    }
}