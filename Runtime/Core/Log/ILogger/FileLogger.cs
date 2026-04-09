using System;
using System.IO;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 文件日志实现类
    /// </summary>
    public class FileLogger : ILogger
    {
        private string filePath;

        public FileLogger(string filePath)
        {
            this.filePath = filePath;
        }

        public void Log(string tag, string message, LogLevel level, string colorHex = null)
        {
            message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {message}\n";
            
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                case LogLevel.Warning:
                case LogLevel.Error:
                    File.AppendAllText(filePath, message);
                    break;
            }
        }

        public void Dispose()
        {
            
        }
    }
}