using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 日志核心管理类
    /// </summary>
    public class LogCore : ICore
    {
        /// <summary>
        /// 日志显示等级
        /// </summary>
        public LogLevel ShowLevel { get; set; } = LogLevel.Debug;
        private readonly List<ILogger> loggers = new List<ILogger>();

        public LogCore(params ILogger[] loggers) => this.loggers.AddRange(loggers);

        /// <summary>
        /// 注入日志器
        /// </summary>
        /// <param name="logger"></param>
        public void InfuseLogger(ILogger logger)
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
        public void Debug(string tag, string message, string colorHex = "#797979")
        {
            if (ShowLevel > LogLevel.Debug) return;
            DispatchLog(tag, message, LogLevel.Debug, colorHex);
        }

        /// <summary>
        /// 信息
        /// </summary>
        public void Info(string tag, string message, string colorHex = "#ffffff")
        {
            if (ShowLevel > LogLevel.Info) return;
            DispatchLog(tag, message, LogLevel.Info, colorHex);
        }

        /// <summary>
        /// 警告
        /// </summary>
        public void Warn(string tag, string message)
        {
            if (ShowLevel > LogLevel.Warning) return;
            DispatchLog(tag, message, LogLevel.Warning, "#ffa500");
        }

        /// <summary>
        /// 错误
        /// </summary>
        public void Error(string tag, string message)
        {
            if (ShowLevel > LogLevel.Error) return;
            DispatchLog(tag, message, LogLevel.Error, "#ff0000");
        }

        #endregion

        private void DispatchLog(string tag, string message, LogLevel level, string colorHex)
        {
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

        /// <summary>
        /// 成功
        /// </summary>
        public void Success(string tag, string message) => Info(tag, message, "#00ff00");

        /// <summary>
        /// 高亮
        /// </summary>
        public void Highlight(string tag, string message) => Info(tag, message, "#00d9ff");

        /// <summary>
        /// 临时
        /// </summary>
        public void Temp(string tag, string message) => Info(tag, message, "#a500ff");

        #endregion

        public void OnShutdown()
        {
            foreach (var logger in loggers)
            {
                logger.Dispose();
            }
        }
    }
}