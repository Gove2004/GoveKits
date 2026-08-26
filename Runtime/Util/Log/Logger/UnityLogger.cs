namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// Unity 控制台日志后端，将日志输出到 UnityEngine.Debug。
    /// 使用 rich text 格式渲染颜色和加粗标签。
    /// Verbose / Debug / Info 映射到 Debug.Log，Warning 映射到 Debug.LogWarning，Error 映射到 Debug.LogError。
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>
        /// 将日志输出到 Unity 控制台，使用 rich text 格式渲染颜色和加粗标签。
        /// Verbose/Debug/Info 映射到 Debug.Log，Warning 映射到 Debug.LogWarning，Error 映射到 Debug.LogError。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="tag">日志标签。</param>
        /// <param name="message">日志消息内容。</param>
        /// <param name="colorHex">颜色的十六进制值，默认为白色。</param>
        public void Log(LogLevel level, string tag, string message, string colorHex = null)
        {
            colorHex ??= "#ffffff";
            message = $"<b>[{tag}]</b> <color={colorHex}>{message}</color>";

            switch (level)
            {
                case LogLevel.Verbose:
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

        /// <summary>
        /// UnityLogger 无外部资源需要释放，Close 为空操作。
        /// </summary>
        public void Close()
        {
        }
    }
}