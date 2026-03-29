using System;
using System.IO;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 文件日志实现类
    /// 将日志信息写入到指定的文本文件中，便于持久化存储和后续分析
    /// 实现 ILogger 接口，可被 LogCore 统一管理
    /// </summary>
    public class FileLogger : ILogger
    {
        /// <summary>
        /// 日志文件完整路径
        /// </summary>
        private string filePath;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filePath">日志文件存储路径</param>
        public FileLogger(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// 记录日志到文件
        /// 所有等级的日志都以相同格式写入文件
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="level">日志等级</param>
        /// <param name="colorHex">颜色参数（文件日志不使用）</param>
        public void Log(string tag, string message, LogLevel level, string colorHex = null)
        {
            // 格式化日志消息：包含时间戳、等级、标签和具体内容
            message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {message}\n";
            
            // 根据日志等级执行写入操作
            // 当前实现中所有等级都写入文件，可根据需求差异化处理
            switch (level)
            {
                case LogLevel.Debug:
                    File.AppendAllText(filePath, message);
                    break;
                case LogLevel.Info:
                    File.AppendAllText(filePath, message);
                    break;
                case LogLevel.Warning:
                    File.AppendAllText(filePath, message);
                    break;
                case LogLevel.Error:
                    File.AppendAllText(filePath, message);
                    break;
            }
        }
    }
}