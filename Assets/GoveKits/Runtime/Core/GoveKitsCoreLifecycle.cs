


using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Config;

namespace GoveKits.Runtime.Core
{
     /// <summary>
    /// GoveKitsCore 的生命周期组件，负责在适当的时机调用
    /// GoveKitsCore.Initialize() 和 GoveKitsCore.Shutdown()。
    /// </summary>
    public class GoveKitsCoreLifecycle : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            // 目前核心模块没有需要初始化的内容，但可以在这里添加全局设置或预热逻辑。
            LogCore.Log(nameof(GoveKitsCoreLifecycle), "GoveKitsCore initialized.");

            ConfigCore.InitAsync().Forget();  // 异步初始化配置系统，不等待完成。
        }

        private void OnDestroy()
        {
            // 目前核心模块没有需要清理的内容，但可以在这里添加全局资源释放或保存逻辑。
            LogCore.Log(nameof(GoveKitsCoreLifecycle), "GoveKitsCore shutdown.");
        }
    }        
}