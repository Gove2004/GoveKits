using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 资源包配置，用于指定包裹名称、加载模式和 CDN 地址。
    /// </summary>
    public class PackageConfig
    {
        /// <summary>
        /// 包裹名称，用于标识和管理资源包。
        /// </summary>
        public string PackageName { get; }
        /// <summary>
        /// 资源加载模式，决定编辑器和生产环境的行为。
        /// </summary>
        public ResLoadMode PlayMode { get; }
        /// <summary>
        /// CDN 主服务器地址。
        /// </summary>
        public string CDN_URL { get; }
        /// <summary>
        /// CDN 备用服务器地址。
        /// </summary>
        public string Fallback_URL { get; }

        /// <summary>
        /// 创建资源包配置。
        /// </summary>
        /// <param name="name">包裹名称。</param>
        /// <param name="mode">加载模式。</param>
        /// <param name="cdn">CDN 主服务器地址。</param>
        /// <param name="fallback">CDN 备用服务器地址。</param>
        internal PackageConfig(string name, ResLoadMode mode, string cdn = "", string fallback = "")
        {
            PackageName = name;
            PlayMode = mode;
            CDN_URL = cdn;
            Fallback_URL = fallback;
        }
    }
}
