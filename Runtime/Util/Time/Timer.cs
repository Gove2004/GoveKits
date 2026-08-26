using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 轻量级定时器对象，承载定时器的状态数据和回调引用。
    /// 由 PoolCore 池化管理，TimeWheel 负责调度执行。
    /// </summary>
    public class Timer : IPoolable
    {
        /// <summary>定时器的全局唯一 ID。</summary>
        public long Id { get; private set; }
        /// <summary>定时器是否处于暂停状态。</summary>
        public bool IsPaused { get; internal set; }
        /// <summary>定时器是否已完成（不再触发）。</summary>
        public bool IsDone { get; internal set; }
        /// <summary>定时器是否已被取消。</summary>
        public bool IsCancelled { get; internal set; }

        /// <summary>定时器触发时执行的回调。</summary>
        internal Action Callback;
        /// <summary>定时器触发间隔（秒）。</summary>
        internal float Interval;
        /// <summary>总共循环次数，-1 表示无限循环。</summary>
        internal int LoopCount;
        /// <summary>目标触发 Tick 编号。</summary>
        internal long TargetTick;
        /// <summary>已执行的轮数。</summary>
        internal int Rounds;
        /// <summary>在 LinkedList 中的链接节点引用。</summary>
        internal LinkedListNode<Timer> LinkNode;
        /// <summary>所属的时间轮。</summary>
        internal TimeWheel BelongsToWheel;
        /// <summary>暂停时剩余的计时时间。</summary>
        internal float RemainingTimeOnPause;

        /// <summary>
        /// 设置定时器的全局唯一 ID。
        /// </summary>
        /// <param name="id">ID 值</param>
        public void SetID(long id) => Id = id;

        /// <summary>
        /// 重置定时器状态，供池回收时调用。
        /// 将所有字段归零或置为初始值。
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

        /// <summary>
        /// 暂停定时器。暂停期间不会触发回调，可通过 Resume 恢复。
        /// </summary>
        public void Pause()
        {
            if (IsPaused || IsDone || IsCancelled || BelongsToWheel == null) return;
            IsPaused = true;
            RemainingTimeOnPause = BelongsToWheel.RemoveAndCalcRemaining(this);
        }

        /// <summary>
        /// 恢复已暂停的定时器，继续倒计时。
        /// </summary>
        public void Resume()
        {
            if (!IsPaused || IsDone || IsCancelled || BelongsToWheel == null) return;
            IsPaused = false;
            BelongsToWheel.Schedule(this, RemainingTimeOnPause);
        }

        /// <summary>
        /// 取消定时器，使其不再触发回调并被回收。
        /// </summary>
        public void Cancel()
        {
            if (IsDone || IsCancelled) return;
            IsCancelled = true;
            if (BelongsToWheel != null && BelongsToWheel.IsProcessing) return;
            BelongsToWheel?.MarkForRemove(this);
        }
    }
}