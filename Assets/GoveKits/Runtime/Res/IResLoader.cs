using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace GoveKits.Res
{
    public interface IResLoader
    {
        // 同步加载
        T LoadSync<T>(string path) where T : Object;

        // 异步加载
        UniTask<T> LoadAsync<T>(string path) where T : Object;

        // 卸载资源
        void Unload(Object asset);
    }


        // 资源加载类型
    public enum ResType
    {
        Resources,
        Addressable,
        AssetBundle
    }

    // 资源缓存信息 (核心：引用计数 + 加载来源)
    public class AssetInfo
    {
        public Object Asset;       // 资源本体
        public string Path;        // 路径
        public ResType LoadType;   // 是谁加载的？(关键，用于卸载)
        public int RefCount;       // 引用计数
    }
}