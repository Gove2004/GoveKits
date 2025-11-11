using System;


namespace GoveKits
{
    public class Timer
    {
        // 时间属性
        private float durationTime; // 持续时间
        private float elapsedTime; // 已经过的时间
        private int loopCount; // 循环次数, -1表示无限循环
        // 状态属性
        public bool IsRunning { get; private set; } // 是否运行中
        // 事件属性
        private event Action onComplete;

        // durationTime持续时间，loops循环次数，-1表示无限循环
        public Timer(float duration, int loops = -1)
        {
            durationTime = duration;
            elapsedTime = 0f;
            IsRunning = false;
            loopCount = loops;
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            elapsedTime += deltaTime;
            if (elapsedTime >= durationTime)
            {
                elapsedTime = 0f;
                onComplete?.Invoke();
                if (loopCount > 0)
                {
                    loopCount--;
                }
                else if (loopCount == 0)
                {
                    IsRunning = false;
                }
                else if (loopCount == -1)
                {
                    // 无限循环，不做任何处理
                }
            }
        }

        // public void Start()
        // {
        //     IsRunning = true;
        //     elapsedTime = 0f;
        // }

        // // 停止计时器
        // public void Stop()
        // {
        //     IsRunning = false;
        // }

        // // 重置计时器
        // public void Reset()
        // {
        //     elapsedTime = 0f;
        // }
    }


    public class TimerID : IEquatable<TimerID>
    {
        private int id;
        private static int nextId = 0;

        public static TimerID GetNextId()
        {
            return new TimerID { id = nextId++ };
        }

        public override int GetHashCode() => id;

        public bool Equals(TimerID other)
        {
            if (other is null) return false;
            return this.GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is TimerID other)
            {
                return Equals(other);
            }
            return false;
        }
    }
}