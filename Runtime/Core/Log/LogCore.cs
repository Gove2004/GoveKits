using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 日志核心管理类
    /// </summary>
    public static class LogCore
    {
        /// <summary>
        /// 日志显示等级
        /// </summary>
        public static LogLevel ShowLevel { get; set; } = LogLevel.Debug;
        private static readonly List<ILogger> loggers = new List<ILogger>();
        public static event Action<string, string, LogLevel> OnLog;

        /// <summary>
        /// 注入日志器
        /// </summary>
        /// <param name="logger"></param>
        public static void InfuseLogger(ILogger logger)
        {
            if (logger == null) return;
            
            if (!loggers.Contains(logger))
            {
                loggers.Add(logger);
            }
        }

        #region 标准日志方法

        /// <summary>
        /// 调试
        /// </summary>
        public static void Debug(string tag, string message, string colorHex = "#797979")
        {
            if (ShowLevel > LogLevel.Debug) return;
            DispatchLog(tag, message, LogLevel.Debug, colorHex);
        }

        /// <summary>
        /// 信息
        /// </summary>
        public static void Info(string tag, string message, string colorHex = "#ffffff")
        {
            if (ShowLevel > LogLevel.Info) return;
            DispatchLog(tag, message, LogLevel.Info, colorHex);
        }

        /// <summary>
        /// 警告
        /// </summary>
        public static void Warning(string tag, string message)
        {
            if (ShowLevel > LogLevel.Warning) return;
            DispatchLog(tag, message, LogLevel.Warning, "#ffa500");
        }

        /// <summary>
        /// 错误
        /// </summary>
        public static void Error(string tag, string message)
        {
            if (ShowLevel > LogLevel.Error) return;
            DispatchLog(tag, message, LogLevel.Error, "#ff0000");
        }

        #endregion

        private static void DispatchLog(string tag, string message, LogLevel level, string colorHex)
        {
            OnLog?.Invoke(tag, message, level);
            
            foreach (var logger in loggers)
            {
                try
                {
                    logger.Log(tag, message, level, colorHex);
                }
                catch (Exception e)
                {
                    throw new Exception($"{logger.GetType().Name} Log Error: {e.Message}");
                }
            }
        }

        #region 扩展方法

        public static void Log(string message) => Info("Log", message);

        /// <summary>
        /// 成功
        /// </summary>
        public static void Success(string tag, string message) => Info(tag, message, "#00ff00");

        /// <summary>
        /// 高亮
        /// </summary>
        public static void Highlight(string tag, string message) => Info(tag, message, "#00d9ff");

        /// <summary>
        /// 临时
        /// </summary>
        public static void Temp(string tag, string message) => Info(tag, message, "#a500ff");

        #endregion

        public static void Clear()
        {
            foreach (var logger in loggers)
            {
                logger.Dispose();
            }
            OnLog = null;
        }
    }
}