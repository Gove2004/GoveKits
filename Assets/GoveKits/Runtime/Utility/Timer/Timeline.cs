


// using System;
// using Unity.VisualScripting;

// namespace GoveKits.Manager
// {
//     // 时间轴
//     public class Timeline
//     {
//         // 每格时间分辨率（秒）
//         private readonly float _tickInterval;
//         // 时间轮槽数量
//         private readonly int _wheelSize;

//         // 当前累计时间（用于按 tickInterval 推进）
//         private float _accumulated = 0f;
//         // 当前槽索引
//         private int _currentIndex = 0;

//         // 槽：每个槽包含若干事件节点
//         private readonly System.Collections.Generic.List<EventNode>[] _slots;

//         // id 生成
//         private long _nextId = 1;

//         /// <summary>
//         /// 创建时间轮 Timeline。
//         /// </summary>
//         /// <param name="tickInterval">时间轮最小分辨率（秒），例如 0.1f</param>
//         /// <param name="wheelSize">槽数量（建议为 64/128/256 等）</param>
//         public Timeline(float tickInterval = 0.1f, int wheelSize = 128)
//         {
//             if (tickInterval <= 0f) throw new ArgumentException("tickInterval must > 0");
//             if (wheelSize <= 0) throw new ArgumentException("wheelSize must > 0");

//             _tickInterval = tickInterval;
//             _wheelSize = wheelSize;
//             _slots = new System.Collections.Generic.List<EventNode>[_wheelSize];
//             for (int i = 0; i < _wheelSize; i++) _slots[i] = new System.Collections.Generic.List<EventNode>(4);
//         }

//         /// <summary>
//         /// 表示一个在时间轮上调度的事件节点。
//         /// </summary>
//         public class EventNode
//         {
//             internal long Id { get; set; }
//             internal Action Callback { get; set; }
//             internal int RemainingRounds { get; set; }
//             internal float RepeatInterval { get; set; } // <=0 表示不重复
//             internal bool Cancelled { get; private set; }

//             /// <summary>
//             /// 取消该事件，不会触发回调（若正在执行则不可阻止）。
//             /// </summary>
//             public void Cancel() => Cancelled = true;
//         }

//         /// <summary>
//         /// 调度一个一次性事件在指定延迟后触发。
//         /// 返回对应的 EventNode，可用于取消或查询。
//         /// </summary>
//         public EventNode Schedule(Action callback, float delaySeconds)
//             => ScheduleInternal(callback, delaySeconds, repeatInterval: -1f);

//         /// <summary>
//         /// 调度事件，可选择重复（repeatInterval>0）。
//         /// </summary>
//         public EventNode Schedule(Action callback, float delaySeconds, float repeatInterval)
//             => ScheduleInternal(callback, delaySeconds, repeatInterval);

//         private EventNode ScheduleInternal(Action callback, float delaySeconds, float repeatInterval)
//         {
//             if (callback == null) throw new ArgumentNullException(nameof(callback));
//             if (delaySeconds < 0f) delaySeconds = 0f;

//             // 计算需要跳过的 ticks
//             int ticks = (int)Math.Ceiling(delaySeconds / _tickInterval);
//             int slot = (_currentIndex + (ticks % _wheelSize)) % _wheelSize;
//             int rounds = ticks / _wheelSize;

//             var node = new EventNode
//             {
//                 Id = System.Threading.Interlocked.Increment(ref _nextId),
//                 Callback = callback,
//                 RemainingRounds = rounds,
//                 RepeatInterval = repeatInterval
//             };

//             _slots[slot].Add(node);
//             return node;
//         }

//         /// <summary>
//         /// 每帧调用以推进时间轴（例如在 TimerManager.Update 中被调用）。
//         /// 支持传入 deltaTime，内部按 tickInterval 推进。若不传入则使用 UnityEngine.Time.deltaTime。
//         /// </summary>
//         public void Update(float deltaTime = -1f)
//         {
//             if (deltaTime <= 0f) deltaTime = UnityEngine.Time.deltaTime;
//             _accumulated += deltaTime;

//             while (_accumulated >= _tickInterval)
//             {
//                 _accumulated -= _tickInterval;
//                 TickOnce();
//             }
//         }

//         // 处理一个 tick：执行当前槽中的到期事件
//         private void TickOnce()
//         {
//             var bucket = _slots[_currentIndex];
//             if (bucket.Count > 0)
//             {
//                 // 拷贝列表以便回调过程中安全修改槽
//                 var copy = bucket.ToArray();
//                 bucket.Clear();

//                 foreach (var node in copy)
//                 {
//                     if (node.Cancelled) continue;
//                     if (node.RemainingRounds > 0)
//                     {
//                         node.RemainingRounds--;
//                         // 放回当前槽（不触发），下次轮到时会继续检查
//                         bucket.Add(node);
//                         continue;
//                     }

//                     // 执行回调
//                     try
//                     {
//                         node.Callback?.Invoke();
//                     }
//                     catch (Exception ex)
//                     {
//                         UnityEngine.Debug.LogException(ex);
//                     }

//                     // 如果需要重复，则重新调度
//                     if (node.RepeatInterval > 0f && !node.Cancelled)
//                     {
//                         // 重新计算 tick 距离
//                         int ticks = (int)Math.Ceiling(node.RepeatInterval / _tickInterval);
//                         int slot = (_currentIndex + (ticks % _wheelSize)) % _wheelSize;
//                         int rounds = ticks / _wheelSize;
//                         node.RemainingRounds = rounds;
//                         _slots[slot].Add(node);
//                     }
//                 }
//             }

//             // 前进到下一个槽
//             _currentIndex = (_currentIndex + 1) % _wheelSize;
//         }

//         /// <summary>
//         /// 取消并从所有槽中移除指定事件。
//         /// </summary>
//         public void Cancel(EventNode node)
//         {
//             if (node == null) return;
//             node.Cancel();
//             // 延迟移除：节点会在下次 Tick 时被跳过并不再重新加入
//         }

//         /// <summary>
//         /// 清空所有计划事件。
//         /// </summary>
//         public void Clear()
//         {
//             for (int i = 0; i < _wheelSize; i++) _slots[i].Clear();
//         }

//         /// <summary>
//         /// 获取目前排程中的事件数量（用于调试）。
//         /// </summary>
//         public int ScheduledCount()
//         {
//             int sum = 0;
//             for (int i = 0; i < _wheelSize; i++) sum += _slots[i].Count;
//             return sum;
//         }

//         /* 示例用法（注释）：
//          * var timeline = new Timeline(0.1f, 128);
//          * timeline.Schedule(() => Debug.Log("hello after 1s"), 1f);
//          * timeline.Schedule(() => Debug.Log("repeat every 2s"), 2f, 2f);
//          * 在 Update 中调用 timeline.Update(Time.deltaTime);
//          */
//     }
// }