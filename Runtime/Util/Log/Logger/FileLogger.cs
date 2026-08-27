using System;
using System.IO;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 文件日志后端，将日志以纯文本格式追加写入指定文件。
    /// 每条日志一行，包含时间戳、日志等级和标签。
    /// 注意：当前使用 AppendAllText 逐条写入，高频日志场景后续可改为缓冲写入。
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string filePath;

        /// <summary>
        /// 创建 FileLogger 实例，自动确保日志文件所在目录存在。
        /// </summary>
        /// <param name="filePath">日志文件的完整路径</param>
        public FileLogger(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// 将日志以纯文本追加写入文件，格式为 "[时间戳] [等级] [标签] 消息"。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="tag">日志标签。</param>
        /// <param name="message">日志消息内容。</param>
        /// <param name="colorHex">颜色十六进制值（文件日志不使用，保留参数）。</param>
        public void Log(LogLevel level, string tag, string message, string colorHex = null)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {message}\n";

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                case LogLevel.Warning:
                case LogLevel.Error:
                    File.AppendAllText(filePath, message);
                    break;
            }
        }

        /// <summary>
        /// FileLogger 使用 AppendAllText 逐条写入，无持久句柄，Close 为空操作。
        /// </summary>
        public void Close()
        {
        }
    }
}