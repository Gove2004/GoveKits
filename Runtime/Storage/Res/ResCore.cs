using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoveKits.Runtime.Storage
{
    public class ResCore : ICore
    {
        private class AssetRecord
        {
            public string Path;
            public Object Asset;
            public int RefCount;
            public bool IsLoading;
            public float UnloadTime; // 计划卸载的时间戳 (Time.realtimeSinceStartup)
            
            // 挂起的异步任务聚合器
            public List<UniTaskCompletionSource<Object>> Awaiters;
        }

        private readonly IResLoader _loader;
        private readonly Dictionary<string, AssetRecord> _records = new();
        
        // 延迟卸载时间（秒）。根据项目内存吃紧程度调节，推荐 5~15 秒。
        public float DelayUnloadTime { get; set; } = 10f; 

        public ResCore(IResLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        #region 同步与异步加载 API

        public ResHandle<T> Load<T>(string path) where T : Object
        {
            var record = GetOrCreateRecord(path);

            // 1. 命中缓存
            if (record.Asset != null)
            {
                Retain(record);
                return new ResHandle<T>(this, path, record.Asset as T);
            }

            if (record.IsLoading)
            {
                Debug.LogError($"[ResCore] 资源 '{path}' 正在异步加载中，不允许打断进行同步加载！");
                return default;
            }

            // 2. 真实加载
            record.Asset = _loader.Load<T>(path);
            if (record.Asset != null)
            {
                Retain(record);
            }
            else
            {
                _records.Remove(path);
            }

            return new ResHandle<T>(this, path, record.Asset as T);
        }

        public async UniTask<ResHandle<T>> LoadAsync<T>(string path, CancellationToken ct = default) where T : Object
        {
            var record = GetOrCreateRecord(path);

            // 1. 命中缓存
            if (record.Asset != null)
            {
                Retain(record);
                return new ResHandle<T>(this, path, record.Asset as T);
            }

            // 2. 防重入（挂起请求）
            if (record.IsLoading)
            {
                record.Awaiters ??= new List<UniTaskCompletionSource<Object>>();
                var tcs = new UniTaskCompletionSource<Object>();
                record.Awaiters.Add(tcs);
                
                var result = await tcs.Task.AttachExternalCancellation(ct);
                Retain(record);
                return new ResHandle<T>(this, path, result as T);
            }

            // 3. 执行加载
            record.IsLoading = true;
            Object asset = null;
            try
            {
                asset = await _loader.LoadAsync<T>(path, ct);
            }
            catch (OperationCanceledException) { /* 忽略取消异常 */ }
            catch (Exception e)
            {
                Debug.LogError($"[ResCore] 加载失败 {path}: {e}");
            }
            finally
            {
                record.IsLoading = false;
                record.Asset = asset;
                
                if (asset != null) Retain(record);
                else _records.Remove(path);

                // 唤醒所有挂起的等待者
                if (record.Awaiters != null)
                {
                    foreach (var awaiter in record.Awaiters)
                    {
                        awaiter.TrySetResult(asset);
                    }
                    record.Awaiters.Clear();
                }
            }

            return new ResHandle<T>(this, path, asset as T);
        }

        #endregion

        #region 自动化生命周期 API (Instantiate)

        /// <summary>
        /// 异步加载并实例化 GameObject，自带生命周期绑定。
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(string path, Transform parent = null, CancellationToken ct = default)
        {
            var handle = await LoadAsync<GameObject>(path, ct);
            if (!handle.IsValid) return null;

            var instance = Object.Instantiate(handle.Asset, parent);
            
            var binder = instance.GetComponent<ResAutoReleaseBinder>();
            if (binder == null) binder = instance.AddComponent<ResAutoReleaseBinder>();
            
            // 绑定委托，当 instance 被 Destroy 时触发 Dispose
            binder.Bind(() => handle.Dispose());

            return instance;
        }

        #endregion

        #region 内部管理与 GC (需要外部 Update 驱动 Tick)

        private AssetRecord GetOrCreateRecord(string path)
        {
            if (!_records.TryGetValue(path, out var record))
            {
                record = new AssetRecord { Path = path };
                _records[path] = record;
            }
            return record;
        }

        private void Retain(AssetRecord record)
        {
            record.RefCount++;
            record.UnloadTime = 0; // 只要有人使用，就取消卸载计划
        }

        internal void ReleaseHandle(string path)
        {
            if (_records.TryGetValue(path, out var record))
            {
                record.RefCount--;
                // 引用归零，开始倒计时
                if (record.RefCount <= 0)
                {
                    record.UnloadTime = Time.realtimeSinceStartup + DelayUnloadTime;
                }
            }
        }

        /// <summary>
        /// 外部主循环调用（如 GameManager.Update 中）
        /// </summary>
        public void Update()
        {
            if (_records.Count == 0) return;

            float currentTime = Time.realtimeSinceStartup;
            List<string> toRemove = null;

            foreach (var kvp in _records)
            {
                var record = kvp.Value;
                // 1. 引用为 0
                // 2. 没有在加载中
                // 3. 被标记为需要卸载 (UnloadTime > 0)
                // 4. 已达到卸载时间
                if (record.RefCount <= 0 && !record.IsLoading && record.UnloadTime > 0 && currentTime >= record.UnloadTime)
                {
                    toRemove ??= new List<string>();
                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var path in toRemove)
                {
                    var record = _records[path];
                    _loader.Unload(path, record.Asset);
                    _records.Remove(path);
                }
            }
        }

        public void OnShutdown()
        {
            foreach (var record in _records.Values)
            {
                if (record.Asset != null)
                    _loader.Unload(record.Path, record.Asset);
            }
            _records.Clear();
            _loader.Clear();
        }
        
        #endregion
    }
}