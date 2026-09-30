using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 日志核心模块，框架内唯一的日志统一调用点。
    ///
    /// 职责：
    /// - 管理日志等级过滤，低于设定等级的日志将被丢弃。
    /// - 管理多个 ILogger 后端，将日志分发到所有已注册的输出目标。
    /// - 在分发前触发 OnLog 事件，供外部拦截或采集日志数据。
    /// - 提供 Verbose / Debug / Info / Warning / Error 及多种便捷方法。
    ///
    /// 扩展方式：
    /// 需要新的输出目标时，实现 ILogger 接口并通过 AddLogger 注入即可，
    /// 无需替换或继承 LogCore 本身。
    /// </summary>
    public static class LogCore
    {
        private static LogLevel logLevel = LogLevel.Debug;
        private static List<ILogger> loggers = new();

        static LogCore()
        {
            ResetForDomainReload();
        }

        // 关闭 Domain Reload 时清理静态状态，避免跨 Play 会话残留导致 AddLogger 抛异常
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReload()
        {
            logLevel = LogLevel.Debug;
            // 关闭 Domain Reload 时旧后端实例不走 Close（如 FileLogger 的退出钩子/句柄），需在此显式释放
            foreach (var logger in loggers)
            {
                try { logger.Close(); }
                catch { /* Reset 阶段无法可靠记录日志，静默 */ }
            }
            loggers.Clear();
            OnLog = null;
        }

        /// <summary>
        /// 日志分发事件。在日志发送给各后端之前触发，
        /// 参数依次为：日志等级、标签、消息内容、颜色代码。
        /// </summary>
        public static event Action<LogLevel, string, string, string> OnLog;

        /// <summary>关闭所有已注册的日志后端（由 GoveCore.Close 调用），同时清空 OnLog 订阅。</summary>
        public static void Close()
        {
            foreach (var logger in loggers)
            {
                try
                {
                    logger.Close();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[LogCore] Logger {logger.GetType().Name} 关闭失败: {e.Message}");
                }
            }
            loggers.Clear();
            OnLog = null;
        }

        /// <summary>
        /// 设置当前最低日志等级。低于此等级的日志将被过滤不输出。
        /// </summary>
        /// <param name="level">新的日志等级</param>
        public static void SetLogLevel(LogLevel level)
        {
            logLevel = level;
        }

        /// <summary>
        /// 向日志核心注入一个日志后端。同一 logger 实例不应重复添加。
        /// </summary>
        /// <param name="logger">要注入的日志后端，不能为 null</param>
        /// <exception cref="ArgumentNullException">logger 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">logger 已存在时抛出</exception>
        public static void AddLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger), "Logger 不能为 null。");
            }
            if (loggers.Contains(logger))
            {
                throw new InvalidOperationException($"Logger {logger.GetType().Name} 已添加过，请勿重复添加。");
            }
            loggers.Add(logger);
        }

        private static void DispatchLog(LogLevel level, string tag, string message, string colorHex)
        {
            if (level < logLevel)
            {
                return;
            }

            // OnLog 订阅者异常必须隔离——EventBus/TimeWheel 的异常兜底里正是 LogCore.Error，
            // 若此处放行异常会击穿全框架的异常隔离
            if (OnLog != null)
            {
                try
                {
                    OnLog(level, tag, message, colorHex);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[LogCore] OnLog 订阅者处理日志异常: {e.Message}");
                }
            }

            for (int i = 0; i < loggers.Count; i++)
            {
                try
                {
                    loggers[i].Log(level, tag, message, colorHex);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[LogCore] Logger {loggers[i].GetType().Name} 日志输出失败: {e.Message}");
                }
            }
        }

        // ── 标准日志方法 ──────────────────────────────────────────

        /// <summary>输出 Verbose 级别日志，用于开发调试的琐碎信息。</summary>
        public static void Verbose(string tag, string message, string colorHex = "#797979")
        {
            DispatchLog(LogLevel.Verbose, tag, message, colorHex);
        }

        /// <summary>输出 Debug 级别日志，用于跟踪程序运行流程。</summary>
        public static void Debug(string tag, string message, string colorHex = "#b8b8b8")
        {
            DispatchLog(LogLevel.Debug, tag, message, colorHex);
        }

        /// <summary>输出 Info 级别日志，用于记录一般性运行时消息。</summary>
        public static void Info(string tag, string message, string colorHex = "#ffffff")
        {
            DispatchLog(LogLevel.Info, tag, message, colorHex);
        }

        /// <summary>输出 Warning 级别日志，用于提示潜在问题但不影响运行。</summary>
        public static void Warning(string tag, string message, string colorHex = "#ff7300")
        {
            DispatchLog(LogLevel.Warning, tag, message, colorHex);
        }

        /// <summary>输出 Error 级别日志，用于记录导致功能异常的问题。</summary>
        public static void Error(string tag, string message, string colorHex = "#ff0000")
        {
            DispatchLog(LogLevel.Error, tag, message, colorHex);
        }

        // ── 便捷方法 ──────────────────────────────────────────────

        /// <summary>以 "Log" 为标签输出 Info 级别日志的便捷方法。</summary>
        public static void Log(string message, string colorHex = "#ffffff")
        {
            DispatchLog(LogLevel.Info, "Log", message, colorHex);
        }

        /// <summary>成功消息便捷方法，输出绿色 Info 级别日志。</summary>
        public static void Success(string tag, string message, string colorHex = "#00ff00")
        {
            DispatchLog(LogLevel.Info, tag, message, colorHex);
        }

        /// <summary>高亮消息便捷方法，输出青色 Info 级别日志。</summary>
        public static void Highlight(string tag, string message, string colorHex = "#00ffff")
        {
            DispatchLog(LogLevel.Info, tag, message, colorHex);
        }

        /// <summary>临时消息便捷方法，输出紫色 Info 级别日志。</summary>
        public static void Temp(string tag, string message, string colorHex = "#ff00ff")
        {
            DispatchLog(LogLevel.Info, tag, message, colorHex);
        }
    }
}
