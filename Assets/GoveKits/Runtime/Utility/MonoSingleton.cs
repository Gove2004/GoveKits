using UnityEngine;



/// <summary>
/// Mono单例基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static T Instance
    {
        get
        {
            // 应用退出检测
            if (_applicationIsQuitting)
            {
                Debug.LogWarning("[MonoSingleton] 实例已销毁");
                return null;
            }

            lock (_lock)  // 线程安全锁
            {
                if (_instance == null)  // 首次访问时创建
                {
                    // 在场景中查找现有实例（包括未激活对象）
                    _instance = (T)FindFirstObjectByType(typeof(T), FindObjectsInactive.Include);

                    // 错误检查：确保只有一个实例
                    if (FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
                    {
                        Debug.LogError("存在多个单例实例！");
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
    }

    /// <summary>
    /// 应用退出时调用
    /// </summary>
    protected virtual void OnDestroy()
    {
        _applicationIsQuitting = true;
    }
}
