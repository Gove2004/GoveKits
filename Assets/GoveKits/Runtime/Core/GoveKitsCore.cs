

using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Config;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// GoveKitsCore 的生命周期组件，负责在适当的时机调用
    /// GoveKitsCore.Initialize() 和 GoveKitsCore.Shutdown()。
    /// </summary>
    public class GoveKitsCoreLifecycle : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            GoveKitsCore.Initialize();
        }

        private void OnDestroy()
        {
            GoveKitsCore.Shutdown();
        }
    }




    
    /// <summary>
    /// GoveKits 核心模块。
    /// </summary>
    public static class GoveKitsCore
    {
        #region Log

        public enum LogType
        {
            Log,
            Warning,
            Error
        }

        /// <summary>
        /// 统一的日志接口，支持标签、消息内容、颜色和日志类型。
        /// </summary>
        /// <param name="tag">日志标签，用于区分不同模块或功能的日志。</param>
        /// <param name="message">日志内容。</param>
        /// <param name="colorHex">日志颜色的十六进制字符串（不带 #），默认为白色 "FFFFFF"。</param>
        /// <param name="logType">日志类型，默认为 LogType.Log。</param>
        public static void Log(string tag, string message, string colorHex = "FFFFFF", LogType logType = LogType.Log)
        {
            string logMessage = $"[{tag}] <color=#{colorHex}>{message}</color>";
            switch (logType)
            {
                case LogType.Log:
                    UnityEngine.Debug.Log(logMessage);
                    break;
                case LogType.Warning:
                    UnityEngine.Debug.LogWarning(logMessage);
                    break;
                case LogType.Error:
                    UnityEngine.Debug.LogError(logMessage);
                    break;
            }
            
        }



        #endregion


        #region Life Cycle

        public static void Initialize()
        {
            // 目前核心模块没有需要初始化的内容，但可以在这里添加全局设置或预热逻辑。
            Log(nameof(GoveKitsCore), "GoveKitsCore initialized.", logType: LogType.Log);

            ConfigCore.InitAsync().Forget();  // 异步初始化配置系统，不等待完成。
        }

        public static void Shutdown()
        {
            // 目前核心模块没有需要清理的内容，但可以在这里添加全局资源释放或保存逻辑。
            Log(nameof(GoveKitsCore), "GoveKitsCore shutdown.", logType: LogType.Log);
        }

        #endregion
    }

    
}