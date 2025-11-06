using System;
using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.Manager
{
    public class TimerManager : MonoSingleton<TimerManager>
    {
        private Dictionary<TimerID, Timer> timerDictionary = new Dictionary<TimerID, Timer>();

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var timer in timerDictionary.Values)
            {
                timer.Update(deltaTime);
            }
        }

        /// <summary>
        /// 创建一个计时器
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="loops">循环次数，-1表示无限循环</param>
        /// <returns></returns>
        public Timer CreateTimer(float duration, int loops = -1)
        {
            Timer timer = new Timer(duration, loops);
            timerDictionary.Add(TimerID.GetNextId(), timer);
            return timer;
        }

        /// <summary>
        /// 停止并移除一个计时器
        /// </summary>
        /// <param name="timer"></param>
        public void RemoveTimer(TimerID timerID)
        {
            if (timerDictionary.ContainsKey(timerID))
            {
                timerDictionary.Remove(timerID);
            }
        }

        /// <summary>
        /// 停止并移除所有计时器
        /// </summary>
        public void RemoveAllTimers()
        {
            timerDictionary.Clear();
        }
    }
}