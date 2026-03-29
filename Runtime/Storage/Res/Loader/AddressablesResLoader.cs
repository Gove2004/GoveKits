using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITASK_ADDRESSABLE_SUPPORT
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace GoveKits.Runtime.Storage.Res
{
    /// <summary>
    /// 基于 Addressables 的资源加载器。
    /// </summary>
    /// <remarks>
    /// 需启用 UNITASK_ADDRESSABLE_SUPPORT 宏并安装 Addressables 包。
    /// </remarks>
    public sealed class AddressablesResLoader : IResLoader
    {
        public ResLoadType LoadType => ResLoadType.Addressable;

        public T Load<T>(string path) where T :  UnityEngine.Object
        {
#if UNITASK_ADDRESSABLE_SUPPORT
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
            handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                return null;
            }

            return handle.Result;
#else
            throw new InvalidOperationException("Addressables package not found. Install com.unity.addressables.");
#endif
        }

        public async UniTask<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
#if UNITASK_ADDRESSABLE_SUPPORT
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
            T result = await handle.ToUniTask(cancellationToken: cancellationToken);
            return result;
#else
            await UniTask.CompletedTask;
            throw new InvalidOperationException("Addressables package not found. Install com.unity.addressables.");
#endif
        }

        public void Unload( UnityEngine.Object asset)
        {
#if UNITASK_ADDRESSABLE_SUPPORT
            if (asset != null)
            {
                Addressables.Release(asset);
            }
#else
            // no-op when Addressables package is unavailable.
#endif
        }
    }
}