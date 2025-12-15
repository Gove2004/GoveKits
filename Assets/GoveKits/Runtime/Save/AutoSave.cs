


using GoveKits.Time;

namespace GoveKits.Save
{
    public class AutoSave
    {
        private float autoSaveInterval = 300f; // 自动保存间隔，单位：秒
        private Timer timer;


        public AutoSave(float intervalInSeconds)
        {
            autoSaveInterval = intervalInSeconds;
            timer = TimerManager.Loop(autoSaveInterval, OnAutoSave);
        }


        public void OnAutoSave()
        {
            // 在这里实现自动保存逻辑
            // 例如，调用保存系统的保存方法
            // SaveSystem.SaveAll();
            LogManager.LogBlue("AutoSave", "Game auto-saved.");
        }
    }
}