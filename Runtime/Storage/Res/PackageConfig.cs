using YooAsset;

namespace GoveKits.Runtime.Storage
{
    public class PackageConfig
    {
        public string PackageName;
        public EPlayMode PlayMode;
        public string CDN_URL;
        public string Fallback_URL;

        public PackageConfig(string name, EPlayMode mode, string cdn = "", string fallback = "")
        {
            PackageName = name;
            PlayMode = mode;
            CDN_URL = cdn;
            Fallback_URL = fallback;
        }
    }
}