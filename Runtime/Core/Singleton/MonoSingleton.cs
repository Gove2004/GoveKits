using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// Unity MonoBehaviour 单例模式基类
    /// 适用于需要挂载到 GameObject 上的 Unity 组件
    /// 自动管理 GameObject 生命周期和场景切换持久化
    /// 支持运行时动态创建和重复实例检测
    /// </summary>
    /// <typeparam name="T">继承此基类的具体单例组件类型</typeparam>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        /// <summary>
        /// 单例容器对象名称
        /// 所有单例 GameObject 将作为此容器的子对象
        /// 便于统一管理和场景切换时持久化
        /// </summary>
        private const string SingletonContainerName = "GoveKitsSingletons";
        
        /// <summary>
        /// 单例实例引用
        /// 静态字段确保全局唯一访问
        /// </summary>
        private static T _instance;

        /// <summary>
        /// 初始化标记
        /// 确保 Init() 方法只被调用一次
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// 单例实例访问入口
        /// 首次访问时自动创建实例（仅在运行时）
        /// 编辑器模式下不会自动创建
        /// </summary>
        public static T Instance
        {
            get
            {
                // 检查实例是否存在且仅在应用运行时创建
                if (_instance == null && Application.isPlaying)
                {
                    CreateInstance();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Unity Awake 生命周期方法
        /// 在组件启用时自动调用
        /// 负责初始化单例实例和设置持久化
        /// </summary>
        protected virtual void Awake()
        {
            // 如果当前实例就是单例实例，则进行初始化
            if (_instance == null || _instance == this)
            {
                if (!_initialized)
                {
                    // 如果是通过属性访问创建的实例，需要将实例引用赋值给静态字段
                    if (_instance == null)
                    {
                        _instance = (T)this;
                    }
                    
                    Init();
                    _initialized = true;
                    
                    // 查找或创建单例容器对象
                    GameObject container = GameObject.Find(SingletonContainerName);
                    if (container == null)
                    {
                        container = new GameObject(SingletonContainerName);
                        // 容器对象跨场景持久化
                        Object.DontDestroyOnLoad(container);
                    }
                    
                    // 将单例对象设为容器的子对象，保持层级整洁
                    transform.SetParent(container.transform);
                }
            }
            else
            {
                // 如果已经存在单例实例，则销毁当前重复实例
                LogCore.Warning(nameof(MonoSingleton<T>), $"检测到重复的 {typeof(T).Name} 实例，已自动销毁。");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 创建单例实例
        /// 优先查找场景中是否已存在该类型组件
        /// 不存在则动态创建 GameObject 并添加组件
        /// 注意：初始化将在 Awake 中进行
        /// </summary>
        private static void CreateInstance()
        {
            // 尝试在场景中查找已存在的实例
            _instance = Object.FindFirstObjectByType<T>();
            if (_instance == null)
            {
                // 创建新的 GameObject 并添加组件
                GameObject obj = new GameObject(typeof(T).Name);
                _instance = obj.AddComponent<T>();
                // Awake 将被自动调用，并在其中进行初始化
            }
            // 如果找到现有实例，Awake 可能已经被调用或稍后被调用
            // 初始化逻辑已在 Awake 中处理
        }

        /// <summary>
        /// Unity OnDestroy 生命周期方法
        /// 在组件销毁时自动调用
        /// 负责清理单例引用和调用反初始化
        /// </summary>
        protected virtual void OnDestroy()
        {
            // 调用反初始化方法
            Uninit();
            
            // 如果销毁的是当前单例实例，清空引用和初始化标记
            if (_instance != null && _instance.gameObject == gameObject)
            {
                _instance = null;
                _initialized = false;
            }
        }

        /// <summary>
        /// 初始化方法
        /// 在单例创建完成后自动调用（通过 Awake）
        /// 子类可重写此方法执行自定义初始化逻辑
        /// </summary>
        protected virtual void Init()
        {
        }

        /// <summary>
        /// 反初始化方法
        /// 在单例销毁前自动调用
        /// 子类可重写此方法执行资源清理逻辑
        /// </summary>
        protected virtual void Uninit()
        {
        }
    }
}