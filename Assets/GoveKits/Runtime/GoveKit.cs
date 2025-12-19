
using GoveKits.Audio;
using GoveKits.Config;
using GoveKits.Localization;
using GoveKits.Network;
using GoveKits.Singleton;
using GoveKits.Times;


namespace GoveKits
{
    /// <summary>
    /// 框架入口单例：负责全局模块初始化与帧更新驱动（如计时器）。
    /// </summary>
    public class GoveKit : MonoSingleton<GoveKit>
    {
        /// <summary>
        /// Unity 生命周期：初始化框架并确保单例就绪。
        /// </summary>
        public void Awake()
        {
            // 确保单例实例化
            _ = Instance;

            // 执行初始化逻辑
            Initialize();
            LogManager.LogGreen("GoveKit", "initialized");
        }


        /// <summary>
        /// Unity 生命周期：逐帧更新（驱动 TimerManager 等）。
        /// </summary>
        public void Update()
        {
            TimerManager.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
        }






        /// <summary>
        /// 初始化各核心子系统（按需启用）。
        /// </summary>
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
