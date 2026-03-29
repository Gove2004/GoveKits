
namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 纯 C# 单例模式基类
    /// 适用于普通 C# 类，不依赖 Unity MonoBehaviour
    /// 采用双重检查锁定（DCL）确保线程安全
    /// 支持延迟初始化（Lazy Initialization）
    /// </summary>
    /// <typeparam name="T">继承此基类的具体单例类型</typeparam>
    public abstract class CSharpSingleton<T> where T : CSharpSingleton<T>, new()
    {
        /// <summary>
        /// 线程同步锁对象
        /// 用于确保多线程环境下实例创建的原子性
        /// </summary>
        private static readonly object _lock = new object();
        
        /// <summary>
        /// 单例实例引用
        /// 私有静态字段，确保全局唯一
        /// </summary>
        private static T _instance;

        /// <summary>
        /// 单例实例访问入口
        /// 首次访问时自动创建实例（懒加载）
        /// 使用双重检查锁定保证线程安全
        /// </summary>
        public static T Instance
        {
            get
            {
                // 第一次检查：避免不必要的锁操作，提升性能
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        // 第二次检查：防止多线程同时通过第一次检查
                        if (_instance == null)
                        {
                            // 创建实例（要求 T 有无参构造函数）
                            _instance = new T();
                            // 统一调用初始化方法
                            _instance.Init();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 销毁单例实例
        /// 释放资源并重置单例引用
        /// </summary>
        public static void DestroyInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    _instance.Uninit();
                    _instance = null;
                }
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