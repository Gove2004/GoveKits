using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
#if UNITASK_ADDRESSABLE_SUPPORT
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace GoveKits.Runtime.Storage
{
    public sealed class AddressablesResLoader : IResLoader
    {
#if UNITASK_ADDRESSABLE_SUPPORT
        // 记录 path 到 Handle 的映射
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new();

        public T Load<T>(string path) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(path);
            handle.WaitForCompletion();
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[path] = handle; // 注意：Addressables 同一路径重复加载返回的是同一Handle
                return handle.Result;
            }

            Addressables.Release(handle);
            return null;
        }

        public async UniTask<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(path);
            try
            {
                T result = await handle.ToUniTask(cancellationToken: ct);
                if (result != null)
                {
                    _handles[path] = handle;
                }
                return result;
            }
            catch
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
        }

        public void Unload(string path, Object asset)
        {
            // 根据路径查出 Handle 释放
            if (_handles.TryGetValue(path, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(path);
            }
        }

        public void Clear()
        {
            foreach (var handle in _handles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            _handles.Clear();
        }
#else
        public T Load<T>(string path) where T : Object => throw new NotSupportedException();
        public UniTask<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : Object => throw new NotSupportedException();
        public void Unload(string path, Object asset) { }
        public void Clear() { }
#endif
    }
}