using GoveKits.Times; // 假设你还有这个模块

namespace GoveKits.Save
{
    public class AutoSave
    {
        private float _interval;
        private Timer _timer; // 假设你的 Timer 类

        public AutoSave(float intervalInSeconds)
        {
            _interval = intervalInSeconds;
            // 假设 TimerManager.Loop 还是可用的
            _timer = TimerManager.Loop(_interval, OnAutoSave);
        }

        public void Stop()
        {
            // 假设 Timer 支持 Stop
            _timer.Cancel();
        }

        private void OnAutoSave()
        {
            // 这里通常触发一个全局事件，或者调用具体的保存逻辑
            // 例如：SaveManager.SaveData(GlobalSaveData.Instance, "global.dat");
            
            // 为了演示，这里假设有一个全局事件中心
            // EventCenter.Broadcast(SaveEvents.AutoSaveTriggered);
            
            LogManager.LogBlue("AutoSave", "Triggered.");
        }
    }
}