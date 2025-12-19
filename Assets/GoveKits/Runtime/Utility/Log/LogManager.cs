using System.Diagnostics; // 用于 Conditional 属性
using UnityEngine;
using Debug = UnityEngine.Debug; // 简化引用

namespace GoveKits
{
    /// <summary>
    /// 日志管理器：提供带标签、颂色、条件编译与上下文支持的日志输出。
    /// 仅在编辑器模式下输出信息日志，警告与错误日志无条件输出。
    /// </summary>
    public static class LogManager
    {


        public static void ShowGUI()
        {
            
        }













        #region 基础日志方法

        /// <summary>
        /// 输出彩色日志（仅编辑器模式）。點擊日志可定位到指定上下文物体。
        /// </summary>
        /// <param name="tag">日志标签（会以粗体显示）。</param>
        /// <param name="message">日志消息。</param>
        /// <param name="context">可选的上下文物体，用于日志定位。</param>
        /// <param name="color">HTML 颜色代码（如 #FF0000）。</param>
        [Conditional("UNITY_EDITOR")] 
        public static void Log(string tag, object message, Object context = null, string color = "#FFFFFF")
        {
            if (context != null) Debug.Log($"<color={color}><b>[{tag}]</b> {message}</color>", context);
            else Debug.Log($"<color={color}><b>[{tag}]</b> {message}</color>");
        }

        #endregion

        #region 常用颜色重载 (方便调用)

        /// <summary>输出绿色日志。</summary>
        [Conditional("UNITY_EDITOR")] public static void LogGreen(string tag, object message, Object context = null) => Log(tag, message, context, "#00FF00");
        /// <summary>输出红色日志。</summary>
        [Conditional("UNITY_EDITOR")] public static void LogRed(string tag, object message, Object context = null) => Log(tag, message, context, "#FF0000");
        /// <summary>输出黄色日志。</summary>
        [Conditional("UNITY_EDITOR")] public static void LogYellow(string tag, object message, Object context = null) => Log(tag, message, context, "#FFFF00");
        /// <summary>输出青色日志。</summary>
        [Conditional("UNITY_EDITOR")] public static void LogCyan(string tag, object message, Object context = null) => Log(tag, message, context, "#00FFFF");
        /// <summary>输出洋红色日志。</summary>
        [Conditional("UNITY_EDITOR")] public static void LogMagenta(string tag, object message, Object context = null) => Log(tag, message, context, "#FF00FF");
        /// <summary>输出蓝色日志。</summary>
        [Conditional("UNITY_EDITOR")] public static void LogBlue(string tag, object message, Object context = null) => Log(tag, message, context, "#0000FF");

        #endregion

        #region 警告与错误 (保留原生行为)

        /// <summary>
        /// 输出警告日志（无条件编译）。
        /// </summary>
        /// <param name="tag">日志标签。</param>
        /// <param name="message">警告消息。</param>
        /// <param name="context">可选上下文物体。</param>
        public static void LogWarning(string tag, object message, Object context = null)
        {
            if (context != null) Debug.LogWarning($"[{tag}] {message}", context);
            else Debug.LogWarning($"[{tag}] {message}");
        }

        /// <summary>
        /// 输出错误日志（无条件编译；即使发布版也输出）。
        /// </summary>
        /// <param name="tag">日志标签。</param>
        /// <param name="message">错误消息。</param>
        /// <param name="context">可选上下文物体。</param>
        public static void LogError(string tag, object message, Object context = null)
        {
            if (context != null) Debug.LogError($"[{tag}] {message}", context);
            else Debug.LogError($"[{tag}] {message}");
        }

        #endregion
    }
}