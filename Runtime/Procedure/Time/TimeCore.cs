
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Procedure
{
    public static class TimeCore
    {
        private static long _idCounter;
        private static TimeWheel _scaledWheel;
        private static TimeWheel _unscaledWheel;

        public static void Initialize(
            float tickPrecision = 0.05f,
            int scaledWheelSize = 512,
            int unscaledWheelSize = 512,
            int warmupPoolTimer = 16,
            int maxPoolTimer = 128
        )
        {
            _scaledWheel = new TimeWheel(tickPrecision, scaledWheelSize);
            _unscaledWheel = new TimeWheel(tickPrecision, unscaledWheelSize);
            PoolCore.Create<Timer>(warmupPoolTimer, maxPoolTimer);
        }

        /// <summary>
        /// 逻辑更新
        /// </summary>
        public static void Update(float deltaTime, float unscaledDeltaTime)
        {
            _scaledWheel.Tick(deltaTime);
            _unscaledWheel.Tick(unscaledDeltaTime);
        }

        /// <summary>
        /// 创建一个一次性定时器
        /// </summary>
        public static Timer Once(float delay, System.Action callback, bool useRealTime = false)
        {
            return CreateInternal(delay, -1, 1, callback, useRealTime);
        }

        /// <summary>
        /// 创建一个循环定时器
        /// </summary>
        public static Timer Loop(float interval, System.Action callback, int loopCount = -1, bool useRealTime = false)
        {
            return CreateInternal(interval, interval, loopCount, callback, useRealTime);
        }

        private static Timer CreateInternal(float delay, float interval, int loop, System.Action callback, bool useRealTime)
        {
            var timer = PoolCore.Get<Timer>();

            timer.SetID(++_idCounter);
            timer.Callback = callback;
            timer.Interval = interval;
            timer.LoopCount = loop;
            timer.UseRealTime = useRealTime;

            var wheel = useRealTime ? _unscaledWheel : _scaledWheel;
            wheel.AddTimer(timer, delay);

            return timer;
        }

        public static void Clear()
        {
            _scaledWheel.Clear();
            _unscaledWheel.Clear();
        }
    }
}