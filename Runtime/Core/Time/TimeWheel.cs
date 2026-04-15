
using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Core
{
    public class TimeWheel
    {
        private readonly float _tickDuration;
        private readonly int _wheelSize;
        private readonly LinkedList<Timer>[] _slots;
        
        private long _currentTick;
        private float _accumulatedTime;
        
        // 待回收队列（避免在遍历过程中回收）
        private readonly Queue<Timer> _recycleQueue = new Queue<Timer>(32);
        // 待移除队列（Cancel 时标记）
        private readonly HashSet<Timer> _pendingRemove = new HashSet<Timer>();
        
        public long CurrentTick => _currentTick;
        public float TickDuration => _tickDuration;
        
        // 标记是否正在处理槽位（用于 Cancel 判断）
        public bool IsProcessing { get; private set; }

        public TimeWheel(float tickDuration = 0.05f, int wheelSize = 512)
        {
            _tickDuration = tickDuration;
            _wheelSize = wheelSize;
            _slots = new LinkedList<Timer>[wheelSize];
            for (int i = 0; i < wheelSize; i++)
                _slots[i] = new LinkedList<Timer>();
        }

        /// <summary>
        /// 添加新定时器。
        /// </summary>
        public void AddTimer(Timer timer, float delay)
        {
            if (delay < 0) delay = 0;
            
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
        /// 重新调度（Resume 或 Loop 时用）。
        /// </summary>
        public void Schedule(Timer timer, float delay)
        {
            // 清除旧节点引用，重新加入
            timer.LinkNode = null;
            AddTimer(timer, delay);
        }

        /// <summary>
        /// 计算剩余时间并从时间轮移除（Pause 用，不回收）。
        /// </summary>
        public float RemoveAndCalcRemaining(Timer timer)
        {
            long ticksRemaining = timer.TargetTick - _currentTick;
            if (ticksRemaining < 0) ticksRemaining = 0;
            
            RemoveFromSlot(timer);
            return ticksRemaining * _tickDuration;
        }

        /// <summary>
        /// 从槽位移除（不回收对象）。
        /// </summary>
        private void RemoveFromSlot(Timer timer)
        {
            if (timer.LinkNode?.List != null)
            {
                timer.LinkNode.List.Remove(timer.LinkNode);
            }
            timer.LinkNode = null;
        }

        /// <summary>
        /// 标记待移除（Cancel 时用，延迟到 Process 时回收）。
        /// </summary>
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
        /// 驱动更新。
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 先处理上一轮残留的待回收对象
            ProcessRecycleQueue();
            
            _accumulatedTime += deltaTime;
            
            while (_accumulatedTime >= _tickDuration)
            {
                _accumulatedTime -= _tickDuration;
                ProcessCurrentSlot();
                _currentTick++;
            }
            
            // 处理本轮产生的待回收对象
            ProcessRecycleQueue();
        }

        /// <summary>
        /// 处理当前槽位的所有定时器。
        /// </summary>
        private void ProcessCurrentSlot()
        {
            IsProcessing = true;
            int slotIndex = (int)(_currentTick % _wheelSize);
            var list = _slots[slotIndex];

            var node = list.First;
            while (node != null)
            {
                var timer = node.Value;
                var next = node.Next;

                // 统一处理入口
                ProcessTimer(timer, list, node);

                node = next;
            }
            
            IsProcessing = false;
        }

        /// <summary>
        /// 统一处理单个定时器的状态机。
        /// </summary>
        private void ProcessTimer(Timer timer, LinkedList<Timer> list, LinkedListNode<Timer> node)
        {
            // 1. 已取消 -> 直接回收
            if (timer.IsCancelled)
            {
                list.Remove(node);
                timer.LinkNode = null;
                EnqueueRecycle(timer);
                return;
            }

            // 2. 还有圈数 -> 减圈数
            if (timer.Rounds > 0)
            {
                timer.Rounds--;
                return;
            }

            // 3. 到达触发点
            list.Remove(node);
            timer.LinkNode = null;

            // 4. 已暂停 -> 不回调，但保留状态等待 Resume
            //    实际上 Pause 时已经从 Wheel 移除了，这里主要是防御性编程
            if (timer.IsPaused)
            {
                EnqueueRecycle(timer);
                return;
            }

            // 5. 执行回调
            ExecuteCallback(timer);

            // 6. 处理后续生命周期
            HandlePostExecute(timer);
        }

        /// <summary>
        /// 执行回调（带异常保护）。
        /// </summary>
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

        /// <summary>
        /// 处理执行后的状态（循环 or 结束）。
        /// </summary>
        private void HandlePostExecute(Timer timer)
        {
            // 检查是否需要循环
            bool shouldLoop = timer.LoopCount != 0 && !timer.IsCancelled;
            
            if (!shouldLoop)
            {
                // 结束生命周期
                timer.IsDone = true;
                EnqueueRecycle(timer);
                return;
            }

            // 处理循环计数
            if (timer.LoopCount > 0) 
                timer.LoopCount--;

            // 检查循环结束后是否还有次数
            if (timer.LoopCount == 0)
            {
                timer.IsDone = true;
                EnqueueRecycle(timer);
                return;
            }

            // 重新调度
            Schedule(timer, timer.Interval);
        }

        /// <summary>
        /// 加入回收队列（延迟回收避免遍历中修改）。
        /// </summary>
        private void EnqueueRecycle(Timer timer)
        {
            timer.BelongsToWheel = null;
            _recycleQueue.Enqueue(timer);
        }

        /// <summary>
        /// 统一回收处理。
        /// </summary>
        private void ProcessRecycleQueue()
        {
            while (_recycleQueue.Count > 0)
            {
                var timer = _recycleQueue.Dequeue();
                PoolCore.Return(timer);
            }
        }

        /// <summary>
        /// 清空所有定时器。
        /// </summary>
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
        }
    }
}