using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage.Res
{
    /// <summary>
    /// 基于 Resources 的资源加载器。
    /// </summary>
    /// <remarks>
    /// 仅对可卸载资源调用 Resources.UnloadAsset；实例对象交由场景生命周期管理。
    /// </remarks>
    public sealed class ResourcesResLoader : IResLoader
    {
        public ResLoadType LoadType => ResLoadType.Resources;

        public T Load<T>(string path) where T : Object
            => Resources.Load<T>(path);

        public async UniTask<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            await request.ToUniTask(cancellationToken: cancellationToken);
            return request.asset as T;
        }

        public void Unload(Object asset)
        {
            if (asset == null)
            {
                return;
            }

            // GameObject/Component 通常由实例生命周期管理，不直接做 UnloadAsset。
            if (asset is GameObject || asset is Component)
            {
                return;
            }

            Resources.UnloadAsset(asset);
        }
    }
}