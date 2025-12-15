#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Res
{
    public class AddressableLoader : IResLoader
    {
        public T LoadSync<T>(string path) where T : Object
        {
#if USE_ADDRESSABLES
            // AA 的同步加载 (Unity 2021+ 支持)
            // 注意：性能会比原生异步略差，但在 Audio 等必须同步的场合很有用
            var op = Addressables.LoadAssetAsync<T>(path);
            T result = op.WaitForCompletion();
            
            // 警告：如果加载失败，Result 为 null
            if (op.Status == AsyncOperationStatus.Failed)
            {
                DebugLogger.LogError("AA", $"Load Failed: {path}");
                return null;
            }
            return result;
#else
            LogManager.LogError("AddressableLoader", "Please define 'USE_ADDRESSABLES' symbols.");
            return null;
#endif
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
#if USE_ADDRESSABLES
            var handle = Addressables.LoadAssetAsync<T>(path);
            await handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                DebugLogger.LogError("AA", $"Load Failed: {path}");
                return null;
            }
#else
            LogManager.LogError("AddressableLoader", "Please define 'USE_ADDRESSABLES' symbols.");
            await UniTask.CompletedTask;
            return null;
#endif
        }

        public void Unload(Object asset)
        {
#if USE_ADDRESSABLES
            // AA 释放资源引用
            Addressables.Release(asset);
#endif
        }
    }
}