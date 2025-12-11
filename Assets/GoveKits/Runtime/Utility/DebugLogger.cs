using System.Diagnostics; // 用于 Conditional 属性
using UnityEngine;
using Debug = UnityEngine.Debug; // 简化引用

namespace GoveKits
{
    public static class DebugLogger
    {
#if UNITY_EDITOR

        #region 基础日志方法

        /// <summary>
        /// 带上下文的日志 (点击日志可定位到物体)
        /// </summary>
        public static void Log(string tag, object message, Object context = null, string color = "#FFFFFF")
        {
            if (context != null) Debug.Log($"<color={color}><b>[{tag}]</b> {message}</color>", context);
            else Debug.Log($"<color={color}><b>[{tag}]</b> {message}</color>");
        }

        #endregion

        #region 常用颜色重载 (方便调用)

        // 示例：Logger.LogGreen("Audio", "BGM played");
        public static void LogGreen(string tag, object message, Object context = null) => Log(tag, message, context, "#00FF00");
        public static void LogRed(string tag, object message, Object context = null) => Log(tag, message, context, "#FF0000");
        public static void LogYellow(string tag, object message, Object context = null) => Log(tag, message, context, "#FFFF00");
        public static void LogCyan(string tag, object message, Object context = null) => Log(tag, message, context, "#00FFFF");
        public static void LogMagenta(string tag, object message, Object context = null) => Log(tag, message, context, "#FF00FF");
        public static void LogBlue(string tag, object message, Object context = null) => Log(tag, message, context, "#0000FF");

        #endregion

        #region 警告与错误 (保留原生行为)

        public static void LogWarning(string tag, object message, Object context = null)
        {
            if (context != null) Debug.LogWarning($"[{tag}] {message}", context);
            else Debug.LogWarning($"[{tag}] {message}");
        }

        // Error 通常不加 Conditional，因为即使是发布版，报错也需要被捕捉
        public static void LogError(string tag, object message, Object context = null)
        {
            if (context != null) Debug.LogError($"[{tag}] {message}", context);
            else Debug.LogError($"[{tag}] {message}");
        }

        internal static void LogWarning(string v)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
#endif
}