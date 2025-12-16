using System;
using System.Collections.Generic;
using ExcelDataReader.Log;


namespace GoveKits.Times
{
    public class TimeWheel
    {
        private readonly float _tickDuration;   // 一格多少秒 (精度)
        private readonly int _wheelSize;        // 轮盘大小 (槽位数)
        private readonly LinkedList<Timer>[] _slots;
        
        private long _currentTick;              // 当前走到了第几个 Tick
        private float _accumulatedTime;         // 累积时间

        public long CurrentTick => _currentTick;
        public float TickDuration => _tickDuration;

        public TimeWheel(float tickDuration = 0.05f, int wheelSize = 512)
        {
            _tickDuration = tickDuration;
            _wheelSize = wheelSize;
            _slots = new LinkedList<Timer>[wheelSize];
            for (int i = 0; i < wheelSize; i++)
            {
                _slots[i] = new LinkedList<Timer>();
            }
        }

        /// <summary>
        /// 放入定时器
        /// </summary>
        public void AddTimer(Timer timer, float delay)
        {
            if (delay < 0) delay = 0;

            // 计算需要多少个 Tick
            long ticks = (long)(delay / _tickDuration);
            // 目标 Tick
            long targetTick = _currentTick + ticks;
            
            // 计算圈数
            // 相对当前槽位的偏移量
            long offset = targetTick % _wheelSize; 
            // 实际槽位索引 (这里可以直接用 targetTick % size，因为是循环数组)
            int slotIndex = (int)(targetTick % _wheelSize);
            
            // 计算圈数：(目标Tick - 当前槽位对应的基准Tick) / 轮盘大小
            // 简化版：我们只存储 timer 里的 Rounds。
            // 实际上 Rounds = ticks / _wheelSize
            timer.Rounds = (int)(ticks / _wheelSize);
            timer.TargetTick = targetTick;
            timer.BelongsToWheel = this;

            // 加入链表，并保存节点引用 (O(1) 移除的关键)
            timer.LinkNode = _slots[slotIndex].AddLast(timer);
        }

        /// <summary>
        /// 重新调度 (用于 Resume 或 Loop)
        /// </summary>
        public void Reschedule(Timer timer, float delay)
        {
            timer.LinkNode = null; // 清除旧引用
            AddTimer(timer, delay);
        }

        /// <summary>
        /// 移除并返回剩余时间 (用于 Pause)
        /// </summary>
        public float RemoveAndGetRemainingTime(Timer timer)
        {
            RemoveTimer(timer);

            // 计算剩余时间
            long ticksRemaining = timer.TargetTick - _currentTick;
            if (ticksRemaining < 0) ticksRemaining = 0;
            return ticksRemaining * _tickDuration;
        }

        /// <summary>
        /// 仅移除
        /// </summary>
        public void RemoveTimer(Timer timer)
        {
            if (timer.LinkNode != null && timer.LinkNode.List != null)
            {
                timer.LinkNode.List.Remove(timer.LinkNode);
            }
            timer.LinkNode = null;
        }

        /// <summary>
        /// 驱动 (Update)
        /// </summary>
        public void Tick(float deltaTime)
        {
            _accumulatedTime += deltaTime;

            // 消耗累积时间，步进 Tick
            while (_accumulatedTime >= _tickDuration)
            {
                _accumulatedTime -= _tickDuration;
                ProcessCurrentSlot();
                _currentTick++;
            }
        }

        private void ProcessCurrentSlot()
        {
            int slotIndex = (int)(_currentTick % _wheelSize);
            var list = _slots[slotIndex];

            var node = list.First;
            while (node != null)
            {
                var timer = node.Value;
                var next = node.Next;

                // 检查取消
                if (timer.IsCancelled)
                {
                    list.Remove(node);
                    timer.LinkNode = null;
                }
                // 检查圈数
                else if (timer.Rounds > 0)
                {
                    timer.Rounds--;
                }
                else
                {
                    // --- 触发 ---
                    list.Remove(node); // 先移除
                    timer.LinkNode = null;

                    if (!timer.IsPaused)
                    {
                        try
                        {
                            timer.Callback?.Invoke();
                        }
                        catch (Exception e) { LogManager.LogError("TimeWheel", $"Timer Callback Error: {e}"); }

                        // 处理循环
                        if (timer.LoopCount != 0 && !timer.IsCancelled)
                        {
                            if (timer.LoopCount > 0) timer.LoopCount--;
                            
                            // 只有没结束才重新加入
                            if (timer.LoopCount != 0)
                            {
                                Reschedule(timer, timer.Interval);
                            }
                        }
                    }
                }

                node = next;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _wheelSize; i++) _slots[i].Clear();
            _currentTick = 0;
            _accumulatedTime = 0;
        }
    }
}