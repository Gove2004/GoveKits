using UnityEngine;

namespace GoveKits.Runtime.Util
{
    internal static class MonoSingletonContainer
    {
        private const string ContainerName = "GoveKitsSingletons";
        private static GameObject _container;

        static MonoSingletonContainer()
        {
            ResetForDomainReload();
        }

        // 关闭 Domain Reload 时清理容器引用，避免跨 Play 会话残留
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReload()
        {
            _container = null;
        }

        public static Transform GetOrCreate()
        {
            if (_container != null)
                return _container.transform;

            _container = GameObject.Find(ContainerName);
            if (_container == null)
            {
                _container = new GameObject(ContainerName);
                Object.DontDestroyOnLoad(_container);
            }

            return _container.transform;
        }
    }

    /// <summary>
    /// MonoBehaviour 单例基类。首次访问 Instance 时从场景查找或自动创建，实例统一挂到 DontDestroyOnLoad 容器下。
    /// 适用于需要 MonoBehaviour 生命周期（Update/协程等）的全局单例。
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private bool _initialized;
        private bool _initFailed;

        /// <summary>单例实例，首次访问时自动创建（场景中已有则复用）。</summary>
        public static T Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                    CreateInstance();

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] 检测到重复的 {typeof(T).Name} 实例，已自动销毁。");
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;

            if (_initialized)
                return;

            try
            {
                Init();
                _initialized = true;
                transform.SetParent(MonoSingletonContainer.GetOrCreate(), true);
            }
            catch (System.Exception e)
            {
                // Init 失败时销毁残骸并回滚单例引用，下次访问 Instance 可重试；
                // 不销毁的话 FindFirstObjectByType 会持续捞到这个 Init 未完成的实例且永不重试
                _initFailed = true;
                if (_instance == this) _instance = null;
                Destroy(gameObject);
                Debug.LogError($"[MonoSingleton] {typeof(T).Name} Init 异常: {e}");
                throw;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                if (_initialized)
                {
                    Uninit();
                    _initialized = false;
                }

                _instance = null;
            }
        }

        private static void CreateInstance()
        {
            // 包含未激活对象，避免场景中存在隐藏单例时重复创建
            _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

            // Init 失败的残骸（已标记待销毁）不可复用，弃用并重建；
            // Destroy 延迟到帧末，同帧内可能仍被 Find 捞到，故在此显式跳过
            if (_instance != null && !_instance._initFailed)
                return;

            if (_instance != null)
                Destroy(_instance.gameObject);

            var go = new GameObject(typeof(T).Name);
            _instance = go.AddComponent<T>();
        }

        /// <summary>首次创建后调用一次，子类可重写。</summary>
        protected virtual void Init() { }

        /// <summary>销毁前调用一次，子类可重写。</summary>
        protected virtual void Uninit() { }
    }
}