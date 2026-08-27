using UnityEngine;

namespace GoveKits.Runtime.Util
{
    internal static class MonoSingletonContainer
    {
        private const string ContainerName = "GoveKitsSingletons";
        private static GameObject _container;

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

            Init();
            _initialized = true;
            transform.SetParent(MonoSingletonContainer.GetOrCreate(), true);
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
            _instance = FindFirstObjectByType<T>();
            if (_instance != null)
                return;

            var go = new GameObject(typeof(T).Name);
            _instance = go.AddComponent<T>();
        }

        /// <summary>首次创建后调用一次，子类可重写。</summary>
        protected virtual void Init() { }

        /// <summary>销毁前调用一次，子类可重写。</summary>
        protected virtual void Uninit() { }
    }
}