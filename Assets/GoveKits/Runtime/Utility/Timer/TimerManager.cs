using GoveKits.Pools;
using UnityEngine;

namespace GoveKits.Time
{
    public static class TimerManager
    {
        // 两个时间轮：一个受 TimeScale 影响，一个不受
        private static TimeWheel _scaledWheel;
        private static TimeWheel _unscaledWheel;

        private static long _idCounter;
        private static bool _isInitialized;

        /// <summary>
        /// 初始化 (在 GoveKit 入口调用)
        /// </summary>
        /// <param name="tickPrecision">精度，建议 0.05f (24fps) 或 0.05f (20fps)</param>
        public static void Initialize(float tickPrecision = 0.05f)
        {
            if (_isInitialized) return;
            
            // 512 个槽位，如果是 0.033s 一格，一圈约 17秒。
            // 超过17秒的任务通过 Rounds 机制处理，性能极高。
            _scaledWheel = new TimeWheel(tickPrecision, 512);
            _unscaledWheel = new TimeWheel(tickPrecision, 512);
            
            _isInitialized = true;
            DebugLogger.LogGreen("TimerManager", "Initialized");
        }

        /// <summary>
        /// 驱动更新 (必须在 MonoBehaviour Update 中调用)
        /// </summary>
        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (!_isInitialized) return;

            _scaledWheel.Tick(deltaTime);
            _unscaledWheel.Tick(unscaledDeltaTime);
        }

        #region API

        /// <summary>
        /// 一次性定时器
        /// </summary>
        public static Timer Once(float delay, System.Action callback, bool useRealTime = false)
        {
            return CreateInternal(delay, -1, 1, callback, useRealTime);
        }

        /// <summary>
        /// 循环定时器
        /// </summary>
        /// <param name="interval">间隔</param>
        /// <param name="callback">回调</param>
        /// <param name="loopCount">次数 (-1 无限)</param>
        public static Timer Loop(float interval, System.Action callback, int loopCount = -1, bool useRealTime = false)
        {
            return CreateInternal(interval, interval, loopCount, callback, useRealTime);
        }

        private static Timer CreateInternal(float delay, float interval, int loop, System.Action callback, bool useRealTime)
        {
            if (!_isInitialized) Initialize();

            Timer timer = Pool.Get<Timer>();
            timer.SetID(++_idCounter);
            timer.Callback = callback;
            timer.Interval = interval;
            timer.LoopCount = loop;
            timer.UseRealTime = useRealTime;

            var targetWheel = useRealTime ? _unscaledWheel : _scaledWheel;
            targetWheel.AddTimer(timer, delay);

            return timer;
        }

        public static void Cancel(Timer timer)
        {
            timer?.Cancel();
        }

        public static void ClearAll()
        {
            _scaledWheel?.Clear();
            _unscaledWheel?.Clear();
        }

        #endregion
    }
}