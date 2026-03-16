using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Core.Singleton
{
    /// <summary>
    /// MonoBehaviour 单例基类： Unity 特化，单线程无需锁。
    /// 自动查找或創建实例，并确保 DontDestroyOnLoad 距場景流棄。
    /// </summary>
    /// <typeparam name="T">单例类型（必须是 MonoSingleton&lt;T&gt; 的渐进类且是 MonoBehaviour）。</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        /// <summary>应用伜开段标志。</summary>
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 单例实例。
        /// - 首次访问时，自动执行查找或创建。
        /// - 应用退出或模式粗八时返回 null。
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
        /// Unity 输出：模式粗八时標記应用退出，防止這之後的 Instance 訪宗不會熾看到粗八。
        /// </summary>
        protected virtual void OnDestroy() => _applicationIsQuitting = true;
    }
}