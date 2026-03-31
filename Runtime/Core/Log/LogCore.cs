using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 日志核心管理类
    /// 提供统一的日志入口，支持多日志器同时输出
    /// 采用静态类设计，全局可访问
    /// </summary>
    public static class LogCore
    {
        /// <summary>
        /// 当前显示的日志等级阈值
        /// 低于此等级的日志将被过滤不显示
        /// 默认显示所有等级日志（Debug = 0）
        /// </summary>
        public static LogLevel ShowLevel { get; set; } = LogLevel.Debug;
        
        /// <summary>
        /// 已注册的日志器列表
        /// 支持同时注入多个日志器（如同时输出到文件和控制台）
        /// </summary>
        private static readonly List<ILogger> loggers = new List<ILogger>();

        /// <summary>
        /// 注入日志器
        /// 将自定义日志器注册到系统中，一旦注入无法删除
        /// </summary>
        /// <param name="logger">要注入的日志器实例</param>
        public static void InfuseLogger(ILogger logger)
        {
            // 空值检查
            if (logger == null) return;
            
            // 避免重复注入
            if (!loggers.Contains(logger))
            {
                loggers.Add(logger);
            }
        }

        #region 标准日志方法

        /// <summary>
        /// 输出调试级别日志
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="colorHex">显示颜色，默认灰色</param>
        public static void Debug(string tag, string message, string colorHex = "#797979")
        {
            // 等级过滤：当前等级高于调试等级时不输出
            if (ShowLevel > LogLevel.Debug) return;
            DispatchLog(tag, message, LogLevel.Debug, colorHex);
        }

        /// <summary>
        /// 输出信息级别日志
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="colorHex">显示颜色，默认白色</param>
        public static void Info(string tag, string message, string colorHex = "#ffffff")
        {
            if (ShowLevel > LogLevel.Info) return;
            DispatchLog(tag, message, LogLevel.Info, colorHex);
        }

        /// <summary>
        /// 输出警告级别日志
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="colorHex">显示颜色，默认橙色</param>
        public static void Warn(string tag, string message)
        {
            if (ShowLevel > LogLevel.Warning) return;
            DispatchLog(tag, message, LogLevel.Warning, "#ffa500");
        }

        /// <summary>
        /// 输出错误级别日志
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="colorHex">显示颜色，默认红色</param>
        public static void Error(string tag, string message)
        {
            if (ShowLevel > LogLevel.Error) return;
            DispatchLog(tag, message, LogLevel.Error, "#ff0000");
        }

        #endregion

        /// <summary>
        /// 派发日志到所有已注册的日志器
        /// 遍历所有日志器并调用其 Log 方法
        /// 包含异常捕获，防止单个日志器故障影响其他日志器
        /// </summary>
        /// <param name="tag">日志标签</param>
        /// <param name="message">日志内容</param>
        /// <param name="level">日志等级</param>
        /// <param name="colorHex">显示颜色</param>
        private static void DispatchLog(string tag, string message, LogLevel level, string colorHex)
        {
            foreach (var logger in loggers)
            {
                try
                {
                    logger.Log(tag, message, level, colorHex);
                }
                catch (Exception e)
                {
                    // 日志器异常时抛出包装异常，便于定位问题
                    throw new Exception($"{logger.GetType().Name} Log Error: {e.Message}");
                }
            }
        }

        #region 扩展方法

        /// <summary>
        /// 输出成功日志（信息级别的绿色日志）
        /// </summary>
        public static void Success(string tag, string message) => Info(tag, message, "#00ff00");

        /// <summary>
        /// 输出高亮日志（信息级别的青色日志）
        /// </summary>
        public static void Highlight(string tag, string message) => Info(tag, message, "#00d9ff");

        /// <summary>
        /// 输出紫色日志（信息级别的紫色日志）
        /// </summary> <param name="tag">日志标签</param>
        public static void Purple(string tag, string message) => Info(tag, message, "#a500ff");

        #endregion
    }
}