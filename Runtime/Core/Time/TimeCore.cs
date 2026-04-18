

using System.Collections.Generic;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Core
{
    public static class TimeCore
    {
        public const string NormalWheelName = "NormalWheel";
        public const string UnscaledWheelName = "UnscaledWheel";
        private static long _idCounter;
        private static Dictionary<string, TimeWheel> _wheels = new Dictionary<string, TimeWheel>();


        public static DisposeAction RigisterWheel(string wheelName, float tickInterval, int size)
        {
            if (_wheels.ContainsKey(wheelName))
            {
                LogCore.Warning(nameof(TimeCore), $"时间轮 {wheelName} 已存在，注册失败。");
                return new DisposeAction(() => { });
            }

            var wheel = new TimeWheel(tickInterval, size);
            _wheels.Add(wheelName, wheel);
            return new DisposeAction(() => { UnrigisterWheel(wheelName); });
        }
        public static void UnrigisterWheel(string wheelName)
        {
            if (_wheels.TryGetValue(wheelName, out TimeWheel wheel))
            {
                wheel.Clear();
                _wheels.Remove(wheelName);
            }
        }

        public static void Initialize(int warmupPoolTimer = 16, int maxPoolTimer = 128)
        {
            PoolCore.Create<Timer>(warmupPoolTimer, maxPoolTimer);
        }


        /// <summary>
        /// 逻辑更新
        /// </summary>
        public static void Update(string wheelName, float deltaTime)
        {
            if (_wheels.TryGetValue(wheelName, out TimeWheel wheel))
            {
                wheel.Tick(deltaTime);
            }
        }

        /// <summary>
        /// 创建一个一次性定时器
        /// </summary>
        public static Timer Once(float delay, System.Action callback, string wheelName = NormalWheelName)
        {
            return CreateInternal(delay, -1, 1, callback, wheelName);
        }

        /// <summary>
        /// 创建一个循环定时器
        /// </summary>
        public static Timer Loop(float interval, System.Action callback, int loopCount = -1, string wheelName = NormalWheelName)
        {
            return CreateInternal(interval, interval, loopCount, callback, wheelName);
        }

        private static Timer CreateInternal(float delay, float interval, int loop, System.Action callback, string wheelName)
        {
            var timer = PoolCore.Get<Timer>();

            timer.SetID(++_idCounter);
            timer.Callback = callback;
            timer.Interval = interval;
            timer.LoopCount = loop;

            var wheel = _wheels[wheelName];
            wheel.AddTimer(timer, delay);

            return timer;
        }

        public static void Clear()
        {
            foreach (var wheel in _wheels.Values)
            {
                wheel.Clear();
            }
            _wheels.Clear();
        }
    }
}