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
        /// 创建单例实例
        /// 优先查找场景中是否已存在该类型组件
        /// 不存在则动态创建 GameObject 并添加组件
        /// 将实例挂载到持久化容器下
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
            }

            // 确保初始化（无论是新创建还是找到现有实例）
            if (!_instance._initialized)
            {
                _instance.Init();
                _instance._initialized = true;

                // 查找或创建单例容器对象
                GameObject gameObject = GameObject.Find(SingletonContainerName);
                if (gameObject == null)
                {
                    gameObject = new GameObject(SingletonContainerName);
                    // 容器对象跨场景持久化
                    Object.DontDestroyOnLoad(gameObject);
                }
                
                // 将单例对象设为容器的子对象，保持层级整洁
                _instance.transform.SetParent(gameObject.transform);
            }   
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
            if (_instance != null && _instance.gameObject == base.gameObject)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 初始化方法
        /// 在单例创建完成后自动调用
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