using System;
using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.Manager
{
    public class TimerManager : MonoSingleton<TimerManager>
    {
        private List<Timer> timers = new List<Timer>();

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                Timer timer = timers[i];
                timer.Update(deltaTime);
                if (timer.IsFinished)
                {
                    timers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 创建一个计时器
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="loop">是否循环</param>
        /// <returns></returns>
        public Timer CreateTimer(float duration, Action onComplete, bool loop = false)
        {
            Timer timer = new Timer(duration, onComplete, loop);
            timers.Add(timer);
            return timer;
        }

        /// <summary>
        /// 停止并移除一个计时器
        /// </summary>
        /// <param name="timer"></param>
        public void RemoveTimer(Timer timer)
        {
            if (timers.Contains(timer))
            {
                timers.Remove(timer);
            }
        }

        /// <summary>
        /// 停止并移除所有计时器
        /// </summary>
        public void RemoveAllTimers()
        {
            timers.Clear();
        }
    }

    public class Timer
    {
        public float Duration { get; private set; } // 持续时间
        public float ElapsedTime { get; private set; } // 已经过的时间
        public bool IsFinished { get; private set; } // 是否完成
        public bool Loop { get; private set; } // 是否循环
        private Action onComplete; // 完成回调

        public Timer(float duration, Action onComplete, bool loop = false)
        {
            Duration = duration;
            ElapsedTime = 0f;
            IsFinished = false;
            Loop = loop;
            this.onComplete = onComplete;
        }

        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            ElapsedTime += deltaTime;
            if (ElapsedTime >= Duration)
            {
                onComplete?.Invoke();
                if (Loop)
                {
                    ElapsedTime = 0f;
                }
                else
                {
                    IsFinished = true;
                }
            }
        }
    }
}