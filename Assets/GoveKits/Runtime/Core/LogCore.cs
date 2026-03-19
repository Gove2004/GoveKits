
namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// GoveKits 核心模块。
    /// </summary>
    public static class LogCore
    {
        public static void LogColor(string tag, string message, string colorHex = "FFFFFF")
        {
            string logMessage = $"[{tag}] <color=#{colorHex}>{message}</color>";
            UnityEngine.Debug.Log(logMessage);
        }

        public static void LogInfo(string tag, string message, string colorHex = "FFFFFF")
        {
            string logMessage = $"[{tag}] <color=#{colorHex}>{message}</color>";
            UnityEngine.Debug.Log(logMessage);
        }

        public static void LogWarning(string tag, string message, string colorHex = "FFFF00")
        {
            string logMessage = $"[{tag}] <color=#{colorHex}>{message}</color>";
            UnityEngine.Debug.LogWarning(logMessage);
        }

        public static void LogError(string tag, string message, string colorHex = "FF0000")
        {
            string logMessage = $"[{tag}] <color=#{colorHex}>{message}</color>";
            UnityEngine.Debug.LogError(logMessage);
        }
    } 
}