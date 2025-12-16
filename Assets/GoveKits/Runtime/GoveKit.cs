
using GoveKits.Audio;
using GoveKits.Config;
using GoveKits.Localization;
using GoveKits.Network;
using GoveKits.Singleton;
using GoveKits.Times;


namespace GoveKits
{
    public class GoveKit : MonoSingleton<GoveKit>
    {
        public void Awake()
        {
            // 确保单例实例化
            _ = Instance;

            // 执行初始化逻辑
            Initialize();
            LogManager.LogGreen("GoveKit", "initialized");
        }


        public void Update()
        {
            TimerManager.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
        }






        private static void Initialize()
        {
            // AudioManager.Initialize();
            ConfigManager.Initialize();
            // LanguageManager.Initialize();
            TimerManager.Initialize();

            // 在这里添加其他全局初始化逻辑...
        }
    }
}
