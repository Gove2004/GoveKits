using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// Unity 控制台日志实现类
    /// </summary>
    public class UnityLogger : ILogger
    {
        public void Log(string tag, string message, LogLevel level, string colorHex = null)
        {
            message = $"<b>[{tag}]</b> <color={colorHex}>{message}</color>";
            
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(message);
                    break;
            }
        }

        public void Dispose()
        {
        }
    }
}