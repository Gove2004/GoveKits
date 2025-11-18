

// Module 是 模块，下面都继承自 Module
// App 是 应用，全局唯一的
// Model View Intent 对应MVI架构
// System 是由 App 管理的子模块，是MVI构成的功能系统






namespace GoveKits.MVI
{
    public interface IApp
    {
        public static IApp Instance
    }






   /// <summary>
    /// 应用 - 全局单例，管理系统
    /// </summary>
    public class App : MonoBehaviour
    {
        private static App instance;
        public static App Instance => instance;

        private Dictionary<string, System> systems = new Dictionary<string, System>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // 初始化所有系统
            RegisterSystem(new UserSystem());
            RegisterSystem(new GameSystem());
            // 添加更多系统...

            foreach (var system in systems.Values)
            {
                system.Initialize();
            }
        }

        // 注册系统
        public void RegisterSystem(System system)
        {
            if (system != null)
            {
                systems[system.ModuleId] = system;
            }
        }

        // 获取系统
        public TSystem GetSystem<TSystem>() where TSystem : System
        {
            foreach (var system in systems.Values)
            {
                if (system is TSystem targetSystem)
                {
                    return targetSystem;
                }
            }
            return null;
        }

        // 处理全局意图
        public void ProcessIntent(IIntent intent)
        {
            foreach (var system in systems.Values)
            {
                system.ProcessIntent(intent);
            }
        }

        private void OnDestroy()
        {
            foreach (var system in systems.Values)
            {
                system.Dispose();
            }
            systems.Clear();

            if (instance == this)
            {
                instance = null;
            }
        }
    }
}