namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 纯 C# 单例基类。
    /// 首次访问 Instance 时创建，DestroyInstance 时销毁。
    /// </summary>
    public abstract class CSharpSingleton<T> where T : CSharpSingleton<T>, new()
    {
        private static T _instance;
        private static bool _initialized;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                if (_instance != null)
                    return _instance;

                _instance = new T();
                _instance.Init();
                _initialized = true;
                return _instance;
            }
        }

        public static void DestroyInstance()
        {
            if (_instance == null)
                return;

            if (_initialized)
            {
                _instance.Uninit();
                _initialized = false;
            }

            _instance = null;
        }

        /// <summary>首次创建后调用一次，子类可重写。</summary>
        protected virtual void Init() { }

        /// <summary>销毁前调用一次，子类可重写。</summary>
        protected virtual void Uninit() { }
    }
}