// ============================================
// TimeCore.cs - 简化版
// ============================================
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Procedure
{
    public class TimeCore : ICore
    {
        private long _idCounter;
        private readonly TimeWheel _scaledWheel;
        private readonly TimeWheel _unscaledWheel;

        public TimeCore(
            float tickPrecision = 0.05f,
            int scaledWheelSize = 512,
            int unscaledWheelSize = 512,
            int warmupPoolTimer = 16,
            int maxPoolTimer = 128
        )
        {
            _scaledWheel = new TimeWheel(tickPrecision, scaledWheelSize);
            _unscaledWheel = new TimeWheel(tickPrecision, unscaledWheelSize);
            CoreLocator.Pool.Create<Timer>(warmupPoolTimer, maxPoolTimer);
        }

        /// <summary>
        /// 逻辑更新
        /// </summary>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            _scaledWheel.Tick(deltaTime);
            _unscaledWheel.Tick(unscaledDeltaTime);
        }

        /// <summary>
        /// 创建一个一次性定时器
        /// </summary>
        public Timer Once(float delay, System.Action callback, bool useRealTime = false)
        {
            return CreateInternal(delay, -1, 1, callback, useRealTime);
        }

        /// <summary>
        /// 创建一个循环定时器
        /// </summary>
        public Timer Loop(float interval, System.Action callback, int loopCount = -1, bool useRealTime = false)
        {
            return CreateInternal(interval, interval, loopCount, callback, useRealTime);
        }

        private Timer CreateInternal(float delay, float interval, int loop, System.Action callback, bool useRealTime)
        {
            var timer = CoreLocator.Pool.Get<Timer>();
            timer.SetID(++_idCounter);
            timer.Callback = callback;
            timer.Interval = interval;
            timer.LoopCount = loop;
            timer.UseRealTime = useRealTime;

            var wheel = useRealTime ? _unscaledWheel : _scaledWheel;
            wheel.AddTimer(timer, delay);

            return timer;
        }

        public void ClearAll()
        {
            _scaledWheel.Clear();
            _unscaledWheel.Clear();
        }

        public void OnShutdown()
        {
            ClearAll();
        }
    }
}