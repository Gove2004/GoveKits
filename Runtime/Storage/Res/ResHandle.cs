using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 资源句柄。外部通过它访问资源，Dispose() 自动扣减引用计数。
    /// 结构体设计，0 GC。
    /// </summary>
    public readonly struct ResHandle<T> : IDisposable where T : Object
    {
        private readonly ResCore _core;
        private readonly string _path;
        
        public readonly T Asset;
        public bool IsValid => Asset != null && _core != null;

        internal ResHandle(ResCore core, string path, T asset)
        {
            _core = core;
            _path = path;
            Asset = asset;
        }

        public void Dispose()
        {
            if (IsValid)
            {
                _core.ReleaseHandle(_path);
            }
        }
        
        // 隐式转换：允许直接用句柄赋值给真实类型
        public static implicit operator T(ResHandle<T> handle) => handle.Asset;
        
        // 显式转 bool：允许直接 if(handle)
        public static implicit operator bool(ResHandle<T> handle) => handle.IsValid;
    }
}