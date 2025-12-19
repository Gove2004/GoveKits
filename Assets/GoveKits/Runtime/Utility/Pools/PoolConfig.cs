

namespace GoveKits.Pools
{
    /// <summary>
    /// 池配置常量。控制 C# 对象池与 Unity 对象池的初始容量、最大尺寸等。
    /// </summary>
    public static class PoolConfig
    {
        /// <summary>C# 对象池宜控容量。</summary>
        public const int DefaultCSharpPoolCapacity = 1;
        /// <summary>C# 对象池最大尺寸（-1 表示无限）。</summary>
        public const int MaxCSharpPoolSize = -1;

        /// <summary>Unity 对象池初始容量。</summary>
        public const int DefaultUnityPoolCapacity = 4;
        /// <summary>Unity 对象池最大尺寸。</summary>
        public const int MaxUnityPoolSize = 16;
        /// <summary>是否启用 Unity 对象池的集合检查（防止重复回收）。</summary>
        public const bool EnableUnityPoolCollectionCheck = true;
    }
}