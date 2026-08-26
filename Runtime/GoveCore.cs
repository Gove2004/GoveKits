using GoveKits.Runtime.Network;
using GoveKits.Runtime.Storage;
using GoveKits.Runtime.UI;
using GoveKits.Runtime.Unit;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// GoveKits 运行时组合根（静态类）。
    /// 负责按正确顺序初始化所有需要参数的 Core。
    /// 所有 Core 均为纯静态类，直接通过类名访问，无需 GoveCore 代理。
    /// </summary>
    public static class GoveCore
    {
        /// <summary>
        /// 按依赖顺序初始化所有 Core。
        /// 必须在首次使用任何 Core 之前调用。
        /// </summary>
        public static void Setup()
        {
            // 1. 基础 Core
            LogCore.AddLogger(new UnityLogger());
            // PoolCore — 懒创建，无需初始化

            // 2. 事件总线
            // EventCore;

            // 3. 时间轮
            TimeCore.Setup();

            // 4. 生成和场景（无需初始化）

            // 5. HTTP（需要创建 HttpEngine）
            HttpCore.Setup();

            // 6. 网络（需要初始化内部组件）
            ClientCore.Setup();
            ServerCore.Setup();

            // 7. 配置表解析器（在 Initialize 前由用户调用 AddParser）

            // 8. 业务初始化（由用户在 GoveCore.Setup() 后按需调用）
            // ResCore.InitPackageAsync(...)
            // AudioCore.Setup()
            // LocalizationCore.Setup()
            // ProtocolCenter.ScanAndRegister()
        }

        /// <summary>
        /// 按逆序关闭所有 Core，释放框架资源。
        /// </summary>
        public static void Close()
        {
            // 逆序关闭（与 Setup 顺序相反）
            LocalizationCore.Close();
            AudioCore.Close();
            HotfixCore.Close();
            SaveCore.Close();
            ConfigCore.Close();
            ResCore.Close();
            PrefsCore.Close();

            ServerCore.Close();
            ClientCore.Close();
            HttpCore.Close();
            UICore.Close();
            SpawnCore.Close();

            TimeCore.Close();
            EventCore.Close();
            PoolCore.Close();
            LogCore.Close();
        }
    }
}
