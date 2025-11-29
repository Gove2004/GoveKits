


namespace GoveKits.Log
{
    public static class MyLog
    {

        public static void Log(string message, params object[] args)
        {
            UnityEngine.Debug.Log($"[GoveKits] {message}");
        }
    }
}