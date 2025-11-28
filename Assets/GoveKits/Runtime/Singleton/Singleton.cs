namespace GoveKits.Singleton
{
    /// <summary>
    /// 纯C#单例基类 线程安全，带有双重检查锁定
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T _instance;
        // 使用一个静态只读对象作为锁
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            _instance.SingletonInit();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化钩子，子类可重写
        /// </summary>
        protected virtual void SingletonInit() { }
    }
}