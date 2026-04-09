using System;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 日志器接口
    /// </summary>
    public interface ILogger : IDisposable
    {
        void Log(string tag, string message, LogLevel level, string colorHex = null);
    }
}