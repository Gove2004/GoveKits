using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Res
{
    public class ResourceLoader : IResLoader
    {
        public T LoadSync<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            await request;
            return request.asset as T;
        }

        public void Unload(Object asset)
        {
            // Resources.UnloadAsset 仅适用于非 GameObject 资源 (Texture, Audio, Material)
            // 如果是 GameObject，调用 UnloadAsset 会报错，只能依赖 Resources.UnloadUnusedAssets()
            if (!(asset is GameObject))
            {
                Resources.UnloadAsset(asset);
            }
        }
    }
}