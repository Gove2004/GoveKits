using YooAsset;

namespace GoveKits.Runtime.Storage
{
    public class PackageConfig
    {
        public string PackageName;
        public ResLoadMode PlayMode;
        public string CDN_URL;
        public string Fallback_URL;

        internal PackageConfig(string name, ResLoadMode mode, string cdn = "", string fallback = "")
        {
            PackageName = name;
            PlayMode = mode;
            CDN_URL = cdn;
            Fallback_URL = fallback;
        }
    }

    public class AutoOfflinePackageConfig : PackageConfig
    {
        public AutoOfflinePackageConfig(string name)
            : base(name, ResLoadMode.AutoOfflineMode)
        {
        }
    }


    public class AutoHostPackageConfig : PackageConfig
    {
        public AutoHostPackageConfig(string name, string cdn, string fallback = "")
            : base(name, ResLoadMode.AutoHostMode, cdn, fallback == "" ? cdn : fallback)
        {
        }
    }
}