namespace GoveKits.Debugger
{
    public static class Logger
    {

        public static void Log(string message, string color = "#FFFFFF")
        {
            UnityEngine.Debug.Log($"<color={color}>{message}</color>");
        }



        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }



        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}