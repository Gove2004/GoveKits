

namespace GoveKits.Pools
{
    /// <summary>
    /// 池配置
    /// </summary>
    public static class PoolConfig
    {
        public const int DefaultCSharpPoolCapacity = 1;
        public const int MaxCSharpPoolSize = -1; // 不限制大小

        public const int DefaultUnityPoolCapacity = 4;
        public const int MaxUnityPoolSize = 16;
        public const bool EnableUnityPoolCollectionCheck = true;
    }
}