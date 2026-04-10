using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    public interface IResLoader
    {
        /// <summary>
        /// 加载资源。
        /// </summary>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        UniTask<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : Object;
        
        /// <summary>
        /// 释放底层资源。
        /// </summary>
        void Unload(string path, Object asset);
        
        void Clear();
    }
}