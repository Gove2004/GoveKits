


namespace GoveKits.Core.Singleton
{
    /// <summary>
    /// C# 单例基类：适用于非 MonoBehaviour 类，线程安全。
    /// </summary>
    /// <typeparam name="T">单例类型（必须是 CSharpSingleton&lt;T&gt; 的渐进类）。</typeparam>
    public abstract class CSharpSingleton<T> where T : CSharpSingleton<T>, new()
    {
        private static readonly object _lock = new object();
        private static T _instance;

        /// <summary>
        /// 单例实例。
        /// - 首次访问时创建。
        /// - 线程安全。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)  // 首次访问时创建
                {
                    lock (_lock)  // 锁定以确保线程安全
                    {
                        if (_instance == null)  // 双重检查锁定
                        {
                            _instance = new T();
                            _instance.OnSingletonInit();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 可选的实例初始化方法，子类可以重写以实现自定义初始化逻辑。
        /// </summary>
        protected abstract void OnSingletonInit();
    }
}