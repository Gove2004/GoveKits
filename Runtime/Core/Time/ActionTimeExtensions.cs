using System;

namespace GoveKits.Runtime.Core
{
    public static class ActionTimeExtensions
    {
        /// <summary>
        /// 延迟执行当前 Action。
        /// 用法：XXXAction.Delay(0.5f);
        /// </summary>
        public static Timer Delay(this Action callback, float delay, string wheelName = TimeCore.NormalWheelName)
        {
            return TimeCore.Once(delay, callback, wheelName);
        }

        /// <summary>
        /// 循环执行当前 Action。
        /// 用法：XXXAction..Loop(1f); // 每隔1秒执行一次，直到手动取消
        /// </summary>
        public static Timer Loop(this Action callback, float interval, int loopCount = -1, string wheelName = TimeCore.NormalWheelName)
        {
            return TimeCore.Loop(interval, callback, loopCount, wheelName);
        }
    }
}
