using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace GoveKits.Manager
{
    /// <summary>
    /// 资源管理器，负责AssetBundle的加载和缓存
    /// </summary>
    public 
    class ABManager : MonoSingleton<ABManager>
    {
        private AssetBundle _mainAB;
        private AssetBundleManifest _manifest;
        private Dictionary<string, AssetBundle> _abCache = new Dictionary<string, AssetBundle>();

        private string StreamingAssetsPath => Application.streamingAssetsPath + "/";

        private string MainABName
        {
            get
            {
#if UNITY_IOS
                return "IOS";
#elif UNITY_ANDROID
                return "Android";
#else
                return "PC";
#endif
            }
        }

        // 初始化主包（首次加载时自动调用）
        private void Initialize()
        {
            if (_mainAB == null)
            {
                _mainAB = AssetBundle.LoadFromFile(StreamingAssetsPath + MainABName);
                _manifest = _mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }
        }

        // 核心加载方法（同步/异步统一入口）
        public void LoadAsset<T>(string abName, string assetName, UnityAction<T> callback, bool async = true) where T : Object
        {
            StartCoroutine(LoadAssetCoroutine(abName, assetName, callback, async));
        }

        private IEnumerator LoadAssetCoroutine<T>(string abName, string assetName, UnityAction<T> callback, bool async) where T : Object
        {
            // 1. 确保主包加载
            Initialize();

            // 2. 加载所有依赖包
            string[] dependencies = _manifest.GetAllDependencies(abName);
            foreach (var dep in dependencies)
            {
                yield return LoadBundle(dep, async);
            }

            // 3. 加载目标AB包
            yield return LoadBundle(abName, async);

            // 4. 加载目标资源
            if (async)
            {
                var request = _abCache[abName].LoadAssetAsync<T>(assetName);
                yield return request;
                HandleResult(request.asset as T, callback);
            }
            else
            {
                HandleResult(_abCache[abName].LoadAsset<T>(assetName), callback);
            }
        }

        private IEnumerator LoadBundle(string abName, bool async)
        {
            if (!_abCache.ContainsKey(abName))
            {
                if (async)
                {
                    _abCache.Add(abName, null); // 标记为正在加载
                    var request = AssetBundle.LoadFromFileAsync(StreamingAssetsPath + abName);
                    yield return request;
                    _abCache[abName] = request.assetBundle;
                }
                else
                {
                    _abCache.Add(abName, AssetBundle.LoadFromFile(StreamingAssetsPath + abName));
                }
            }
            else if (_abCache[abName] == null) // 等待异步加载完成
            {
                while (_abCache[abName] == null)
                {
                    yield return null;
                }
            }
        }

        private void HandleResult<T>(T obj, UnityAction<T> callback) where T : Object
        {
            if (obj is GameObject go)
            {
                callback(Instantiate(go) as T);
            }
            else
            {
                callback(obj);
            }
        }

        public void Unload(string abName, bool unloadAllObjects = false)
        {
            if (_abCache.TryGetValue(abName, out var ab))
            {
                ab.Unload(unloadAllObjects);
                _abCache.Remove(abName);
            }
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            AssetBundle.UnloadAllAssetBundles(false);
            _abCache.Clear();
            _mainAB = null;
            _manifest = null;
        }
    }
}
