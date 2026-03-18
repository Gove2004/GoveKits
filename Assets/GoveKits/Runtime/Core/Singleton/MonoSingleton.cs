using UnityEngine;

namespace GoveKits.Runtime.Core.Singleton
{
    /// <summary>
    /// MonoBehaviour 单例基类：Unity 特化，单线程无需锁。
    /// 自动查找或创建实例，并通过 DontDestroyOnLoad 确保跨场景保留。
    /// </summary>
    /// <typeparam name="T">单例类型（必须是 MonoSingleton&lt;T&gt; 的派生类且是 MonoBehaviour）。</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        /// <summary>应用退出标志。</summary>
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 单例实例。
        /// - 首次访问时，自动执行查找或创建。
        /// - 应用退出或播放模式切换时返回 null。
        /// </summary>
        public static T Instance
        {
            get
            {
                // 应用退出检测
                if (_applicationIsQuitting)
                {
                    GoveKitsCore.Log("MonoSingleton", "实例已销毁");
                    return null;
                }

                if (_instance == null)  // 首次访问时创建
                {
                    // 在场景中查找现有实例（包括未激活对象）
                    _instance = (T)FindFirstObjectByType(typeof(T), FindObjectsInactive.Include);

                    // 错误检查：确保只有一个实例
                    if (FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
                    {
                        GoveKitsCore.Log("MonoSingleton", "存在多个单例实例！");
                    }

                    // 如果不存在则创建新实例
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();
                    }
                    DontDestroyOnLoad(_instance);  // 跨场景保留
                }
                return _instance;
            }
        }

        /// <summary>
        /// 对象销毁时标记应用退出，防止退出阶段再次创建新实例。
        /// </summary>
        protected virtual void OnDestroy() => _applicationIsQuitting = true;
    }
}