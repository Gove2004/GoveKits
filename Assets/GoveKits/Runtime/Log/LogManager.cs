


namespace GoveKits.Log
{
    public static class LogManager
    {
        public static void Log(string message, params object[] args)
        {
            UnityEngine.Debug.Log($"[GoveKits] {message}");
        }
    }
}