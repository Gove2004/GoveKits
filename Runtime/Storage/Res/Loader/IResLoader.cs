using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage.Res
{
    /// <summary>
    /// 资源加载器接口。
    /// </summary>
    /// <remarks>
    /// 每个加载器负责一种来源，ResCore 负责上层缓存与引用计数。
    /// </remarks>
    public interface IResLoader
    {
        /// <summary>
        /// 当前加载器对应加载类型。
        /// </summary>
        ResLoadType LoadType { get; }

        /// <summary>
        /// 同步加载资源。
        /// </summary>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        UniTask<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : Object;

        /// <summary>
        /// 释放资源。
        /// </summary>
        void Unload(Object asset);
    }
}