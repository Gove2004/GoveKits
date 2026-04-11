using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    public sealed class ResourcesResLoader : IResLoader
    {
        public T Load<T>(string path) where T : Object
        {
            path = path.Substring(0, path.LastIndexOf('.'));  // 去掉扩展名
            return Resources.Load<T>(path);
        }

        public async UniTask<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : Object
        {
            path = path.Substring(0, path.LastIndexOf('.'));  // 去掉扩展名
            var request = Resources.LoadAsync<T>(path);
            await request.ToUniTask(cancellationToken: ct);
            return request.asset as T;
        }

        public void Unload(string path, Object asset)
        {
            if (asset == null) return;
            // Resources 只能卸载非 GameObject 和 Component 类型的资源
            if (asset is not GameObject && asset is not Component)
            {
                Resources.UnloadAsset(asset);
            }
        }

        public void Clear()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}