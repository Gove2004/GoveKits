using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 主机模式包配置。编辑器中使用模拟模式，生产环境中使用 CDN 进行热更新。
    /// </summary>
    public class AutoHostPackageConfig : PackageConfig
    {
        /// <summary>
        /// 创建主机模式包配置。
        /// </summary>
        /// <param name="name">包裹名称。</param>
        /// <param name="cdn">CDN 主服务器地址。</param>
        /// <param name="fallback">CDN 备用服务器地址，默认为空时使用 cdn 值。</param>
        public AutoHostPackageConfig(string name, string cdn, string fallback = "")
            : base(name, ResLoadMode.AutoHostMode, cdn, fallback == "" ? cdn : fallback) { }
    }
}
