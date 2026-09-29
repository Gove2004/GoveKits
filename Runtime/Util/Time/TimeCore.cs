using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 时间轮封装，对外提供一次性定时器、循环定时器和暂停恢复等 API。
    /// 内部基于 TimeWheel 实现，支持高精度定时调度。
    /// </summary>
    public static class TimeCore
    {
        private static TimeWheel wheel;
        private static long idCounter;

        static TimeCore()
        {
            ResetForDomainReload();
        }

        // 关闭 Domain Reload 时清理静态状态，避免跨 Play 会话残留
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReload()
        {
            wheel = null;
            idCounter = 0;
        }

        /// <summary>
        /// 初始化时间轮系统。
        /// 可在首次使用定时器之前调用以自定义精度；未调用时首次创建定时器会按默认参数自动初始化。
        /// </summary>
        /// <param name="tickDuration">每次 tick 的时间跨度（秒），默认 50ms</param>
        /// <param name="wheelSize">时间轮的槽位数，默认 512</param>
        public static void Setup(float tickDuration = 0.05f, int wheelSize = 512)
        {
            wheel = new TimeWheel(tickDuration, wheelSize);
        }

        /// <summary>
        /// 驱动时间轮前进，应在每帧更新时调用。
        /// 所有到期定时器将在此时触发回调。
        /// </summary>
        /// <param name="deltaTime">距上一帧的增量时间（秒）</param>
        public static void Tick(float deltaTime)
        {
            wheel?.Tick(deltaTime);
        }

        /// <summary>
        /// 创建一次性定时器，在指定延迟后执行回调，仅触发一次。
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">超时回调</param>
        /// <returns>定时器实例，可用于暂停、恢复或取消</returns>
        public static Timer Once(float delay, Action callback)
        {
            return CreateTimer(delay, 0, 1, callback);
        }

        /// <summary>
        /// 创建循环定时器，按指定间隔重复执行回调。
        /// loopCount 为正数时执行指定次数后自动停止；为 -1 时无限循环。
        /// </summary>
        /// <param name="interval">触发间隔（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="loopCount">循环次数，-1 表示无限循环</param>
        /// <returns>定时器实例</returns>
        public static Timer Loop(float interval, Action callback, int loopCount = -1)
        {
            return CreateTimer(interval, interval, loopCount, callback);
        }

        /// <summary>
        /// 清空所有定时器并释放时间轮资源。
        /// 通常在场景切换或应用退出时调用。
        /// </summary>
        public static void Close()
        {
            wheel?.Clear();
            wheel = null;
        }

        private static Timer CreateTimer(float delay, float interval, int loopCount, Action callback)
        {
            // 未显式 Setup 时按默认参数自动初始化，并提示 Tick 需要驱动
            if (wheel == null)
            {
                LogCore.Warning(nameof(TimeCore), "尚未调用 TimeCore.Setup，已按默认参数自动初始化。请确保每帧调用 TimeCore.Tick(Time.deltaTime) 驱动时间轮。");
                Setup();
            }

            var timer = PoolCore.Get<Timer>();
            timer.SetID(++idCounter);
            timer.Callback = callback;
            timer.Interval = interval;
            timer.LoopCount = loopCount;
            wheel.AddTimer(timer, delay);
            return timer;
        }
    }
}
