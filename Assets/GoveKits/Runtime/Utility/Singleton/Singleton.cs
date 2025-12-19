namespace GoveKits.Singleton
{
    /// <summary>
    /// 纯 C# 单例基类：线程安全，采用双重検查锁定优化初始化性能。
    /// ⏾ 需要纳排 T 有无参构造函新。
    /// </summary>
    /// <typeparam name="T">单例类型（必须是 Singleton&lt;T&gt; 的渐进类）。</typeparam>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T _instance;
        /// <summary>锁定对象（线程安全）。</summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 较程式实例（线程安全的胶男实现）。
        /// </summary>
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
        /// 初始化钟子，需要时或子类可以重写以实现自定义初始化逻辑。
        /// </summary>
        protected virtual void SingletonInit() { }
    }
}