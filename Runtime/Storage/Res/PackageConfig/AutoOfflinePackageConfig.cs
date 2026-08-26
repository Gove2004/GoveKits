using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 离线模式包配置。编辑器中使用模拟模式，生产环境中使用离线内置资源。
    /// </summary>
    public class AutoOfflinePackageConfig : PackageConfig
    {
        /// <summary>
        /// 创建离线模式包配置。
        /// </summary>
        /// <param name="name">包裹名称。</param>
        public AutoOfflinePackageConfig(string name)
            : base(name, ResLoadMode.AutoOfflineMode) { }
    }
}
