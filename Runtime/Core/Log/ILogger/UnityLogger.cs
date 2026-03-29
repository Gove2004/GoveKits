using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// Unity 控制台日志实现类
    /// 将日志信息输出到 Unity 控制台，支持富文本格式显示
    /// 实现 ILogger 接口，可被 LogCore 统一管理
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>
        /// 记录日志到 Unity 控制台
        /// 根据日志等级调用不同的 Unity Debug 方法
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="level">日志等级</param>
        /// <param name="colorHex">日志文字颜色（十六进制格式）</param>
        public void Log(string tag, string message, LogLevel level, string colorHex = null)
        {
            // 格式化消息：标签加粗，内容带颜色
            message = $"<b>[{tag}]</b> <color={colorHex}>{message}</color>";
            
            // 根据日志等级调用对应的 Unity Debug 方法
            // Debug 和 Info 使用普通日志，Warning 和 Error 使用对应级别日志
            switch (level)
            {
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(message);
                    break;
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
    }
}