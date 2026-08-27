using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 时间轮定时器调度器，基于环形槽位 + 链表实现高精度定时。
    /// 支持暂停、恢复、取消和循环定时器。
    /// 定时器对象由 PoolCore 池化管理以减少 GC 压力。
    /// </summary>
    public class TimeWheel
    {
        /// <summary>每次 tick 的时间跨度（秒），决定定时精度。</summary>
        private readonly float _tickDuration;
        /// <summary>环形槽位数量，决定单轮可容纳的 tick 数。</summary>
        private readonly int _wheelSize;
        /// <summary>环形槽位链表数组，每个槽位挂载该 tick 到期的定时器。</summary>
        private readonly LinkedList<Timer>[] _slots;

        /// <summary>当前已推进的 tick 总数（单调递增）。</summary>
        private long _currentTick;
        /// <summary>未满一个 tick 的累积增量时间。</summary>
        private float _accumulatedTime;

        /// <summary>待回收定时器队列，统一归还 PoolCore 以复用 Timer 对象。</summary>
        private readonly Queue<Timer> _recycleQueue = new(32);
        /// <summary>
        /// 在处理当前槽位期间通过 AddTimer 加入的新定时器暂存于此。
        /// 等当前槽位处理完毕后再统一插入目标位置，避免修改正在遍历的链表。
        /// </summary>
        private readonly List<(Timer timer, float delay)> _pendingTimers = new();

        /// <summary>当前时间轮已经过的 tick 总数。</summary>
        public long CurrentTick => _currentTick;
        /// <summary>每次 tick 的时间跨度（秒）。</summary>
        public float TickDuration => _tickDuration;
        /// <summary>是否正在处理某个槽位的定时器。</summary>
        public bool IsProcessing { get; private set; }

        /// <summary>
        /// 创建指定精度和容量的时间轮。
        /// </summary>
        /// <param name="tickDuration">每次 tick 的时间跨度（秒），默认 50ms</param>
        /// <param name="wheelSize">环形槽位数量，默认 512</param>
        public TimeWheel(float tickDuration = 0.05f, int wheelSize = 512)
        {
            _tickDuration = tickDuration;
            _wheelSize = wheelSize;
            _slots = new LinkedList<Timer>[wheelSize];
            for (int i = 0; i < wheelSize; i++)
                _slots[i] = new LinkedList<Timer>();
        }

        /// <summary>
        /// 向时间轮中添加一个定时器。
        /// 若在 Tick 处理过程中调用，定时器会被暂存并在当前槽位处理完后插入。
        /// </summary>
        /// <param name="timer">要调度的定时器对象</param>
        /// <param name="delay">延迟时间（秒），负数自动修正为零</param>
        public void AddTimer(Timer timer, float delay)
        {
            if (delay < 0) delay = 0;

            if (IsProcessing)
            {
                // 正在处理 slot 时，延迟添加到目标 slot，避免修改正在遍历的链表
                _pendingTimers.Add((timer, delay));
                return;
            }

            InsertTimer(timer, delay);
        }

        private void InsertTimer(Timer timer, float delay)
        {
            long ticks = (long)(delay / _tickDuration);
            long targetTick = _currentTick + ticks;

            timer.Rounds = (int)(ticks / _wheelSize);
            timer.TargetTick = targetTick;
            timer.BelongsToWheel = this;
            timer.IsDone = false;
            timer.IsCancelled = false;
            timer.IsPaused = false;

            int slotIndex = (int)(targetTick % _wheelSize);
            timer.LinkNode = _slots[slotIndex].AddLast(timer);
        }

        /// <summary>
        /// 安排一个定时器在指定延迟后执行。
        /// 会先清除定时器原有的链表节点引用。
        /// </summary>
        /// <param name="timer">要安排的定时器</param>
        /// <param name="delay">延迟时间（秒）</param>
        public void Schedule(Timer timer, float delay)
        {
            timer.LinkNode = null;
            AddTimer(timer, delay);
        }

        /// <summary>
        /// 从时间轮中移除定时器并计算剩余时间。
        /// </summary>
        /// <param name="timer">要移除的定时器</param>
        /// <returns>剩余等待时间（秒）</returns>
        public float RemoveAndCalcRemaining(Timer timer)
        {
            long ticksRemaining = timer.TargetTick - _currentTick;
            if (ticksRemaining < 0) ticksRemaining = 0;

            RemoveFromSlot(timer);
            return ticksRemaining * _tickDuration;
        }

        private void RemoveFromSlot(Timer timer)
        {
            if (timer.LinkNode?.List != null)
            {
                timer.LinkNode.List.Remove(timer.LinkNode);
            }
            timer.LinkNode = null;
        }

        /// <summary>
        /// 标记定时器为已取消并从链表中移除，加入回收队列。
        /// </summary>
        /// <param name="timer">要取消的定时器</param>
        public void MarkForRemove(Timer timer)
        {
            if (timer.LinkNode?.List != null)
            {
                timer.LinkNode.List.Remove(timer.LinkNode);
                timer.LinkNode = null;
            }
            timer.IsCancelled = true;
            _recycleQueue.Enqueue(timer);
        }

        /// <summary>
        /// 驱动时间轮前进指定增量时间，触发到期的定时器回调。
        /// </summary>
        /// <param name="deltaTime">距上一帧的增量时间（秒）</param>
        public void Tick(float deltaTime)
        {
            ProcessRecycleQueue();

            _accumulatedTime += deltaTime;

            while (_accumulatedTime >= _tickDuration)
            {
                _accumulatedTime -= _tickDuration;
                ProcessCurrentSlot();
                _currentTick++;
            }

            // 将延迟添加的定时器插入目标 slot
            FlushPendingTimers();
            ProcessRecycleQueue();
        }

        private void FlushPendingTimers()
        {
            for (int i = 0; i < _pendingTimers.Count; i++)
            {
                var (timer, delay) = _pendingTimers[i];
                InsertTimer(timer, delay);
            }
            _pendingTimers.Clear();
        }

        private void ProcessCurrentSlot()
        {
            IsProcessing = true;
            try
            {
                int slotIndex = (int)(_currentTick % _wheelSize);
                var list = _slots[slotIndex];

                var node = list.First;
                while (node != null)
                {
                    var timer = node.Value;
                    var next = node.Next;

                    ProcessTimer(timer, list, node);

                    node = next;
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ProcessTimer(Timer timer, LinkedList<Timer> list, LinkedListNode<Timer> node)
        {
            if (timer.IsCancelled)
            {
                list.Remove(node);
                timer.LinkNode = null;
                EnqueueRecycle(timer);
                return;
            }

            if (timer.Rounds > 0)
            {
                timer.Rounds--;
                return;
            }

            list.Remove(node);
            timer.LinkNode = null;

            if (timer.IsPaused)
            {
                Schedule(timer, timer.Interval);
                return;
            }

            ExecuteCallback(timer);
            HandlePostExecute(timer);
        }

        private void ExecuteCallback(Timer timer)
        {
            try
            {
                timer.Callback?.Invoke();
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(TimeWheel), $"Timer[{timer.Id}] Callback Error: {e}");
            }
        }

        private void HandlePostExecute(Timer timer)
        {
            bool shouldLoop = timer.LoopCount != 0 && !timer.IsCancelled;

            if (!shouldLoop)
            {
                timer.IsDone = true;
                EnqueueRecycle(timer);
                return;
            }

            if (timer.LoopCount > 0)
                timer.LoopCount--;

            if (timer.LoopCount == 0)
            {
                timer.IsDone = true;
                EnqueueRecycle(timer);
                return;
            }

            Schedule(timer, timer.Interval);
        }

        private void EnqueueRecycle(Timer timer)
        {
            timer.BelongsToWheel = null;
            _recycleQueue.Enqueue(timer);
        }

        private void ProcessRecycleQueue()
        {
            while (_recycleQueue.Count > 0)
            {
                var timer = _recycleQueue.Dequeue();
                PoolCore.Return(timer);
            }
        }

        /// <summary>清空时间轮中所有定时器并释放资源。</summary>
        public void Clear()
        {
            for (int i = 0; i < _wheelSize; i++)
            {
                var list = _slots[i];
                var node = list.First;
                while (node != null)
                {
                    var timer = node.Value;
                    var next = node.Next;

                    timer.IsCancelled = true;
                    EnqueueRecycle(timer);

                    node = next;
                }
                list.Clear();
            }

            ProcessRecycleQueue();
            _currentTick = 0;
            _accumulatedTime = 0;
            _pendingTimers.Clear();
        }
    }
}
