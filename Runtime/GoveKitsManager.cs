
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Network;
using GoveKits.Runtime.Storage;
using System;
using System.Collections.Generic;
using YooAsset;
using MessagePack;
using MessagePack.Resolvers;

namespace GoveKits.Runtime
{
    public class GoveKitsManager : MonoSingleton<GoveKitsManager>
    {
    
        #region 生命周期

        private void Awake() => Initialize();
        private void Initialize()
        {
            // Dependency
            YooAssets.Initialize(new YooLogger());

            // Core
            LogCore.InfuseLogger(new UnityLogger());
            RandomCore.Initialize(new NormalRNG(Environment.TickCount));
            TimeCore.Initialize(16, 128);
            TimeCore.RigisterWheel(TimeCore.NormalWheelName, 0.05f, 512);
            TimeCore.RigisterWheel(TimeCore.UnscaledWheelName, 0.05f, 512);

            // Network
            // 1. 外部设置 Resolver
            ProtocolCore.AddResolver(GoveKitsBuildinResolver.Instance);
            // 2. 扫描并注册协议 ID 映射（握手/同步/RPC 都依赖它）
            ProtocolCore.ScanAndRegister();
            // 3. 客户端连接
            // await ClientCore.ConnectAsync("127.0.0.1", 3000);
            // 4. （可选）服务端启动
            // await ServerCore.StartAsync(3000);
            // 5. （可选）帧同步组件
            // FrameCore.StartClient();
            // 6. （可选）Rpc组件
            // RpcCore.Initialize();

            // Storage
            // 1. 资源热更新
            // await ResCore.PackageWorkflowAsync(new AutoOfflinePackageConfig("DefaultPackage"), new UpdateCallbacks());
            // 2. 加载 AOT 泛型元数据
            // await HotfixCore.LoadAotMetadataAsync({ "XXX.dll", "YYY.dll" });
            // 3. 加载 热更新 程序集
            // await HotfixCore.LoadHotfixAssemblyAsync("Hotfix.dll");
            // 4. 加载资源
            ConfigCore.InfuseParser(new JsonConfigParser());
            ConfigCore.InfuseParser(new CsvConfigParser());
            ConfigCore.Initialize();
            // 5. 使用资源
            LocalizationCore.Initialize();
            AudioCore.Initialize(16);
            SaveCore.Initialize(new JsonSerializer());

            // Log
            LogCore.Success(nameof(GoveKitsManager), "GoveKits initialized.");
        }


        private void Update()
        {
            // 驱动时间轮
            TimeCore.Update(TimeCore.NormalWheelName, UnityEngine.Time.deltaTime);
            TimeCore.Update(TimeCore.UnscaledWheelName, UnityEngine.Time.unscaledDeltaTime);

            // 驱动帧同步核心
            SyncCore.Update(UnityEngine.Time.deltaTime);
        }


        private void OnApplicationQuit()
        {
            // 依赖框架
            YooAssets.Destroy();

            // 清理各个Core
            // 略
        }

        #endregion
    }







    /// <summary>
    /// 既然YooAsset支持深入日志，那就这样做
    /// </summary>
    public class YooLogger : YooAsset.ILogger
    {
        public void Error(string message)
        {
            LogCore.Error(nameof(YooAssets), message);
        }

        public void Exception(Exception exception)
        {
            LogCore.Error(nameof(YooAssets), exception.ToString());
        }

        public void Log(string message)
        {
            LogCore.Info(nameof(YooAssets), message);
        }

        public void Warning(string message)
        {
            LogCore.Warning(nameof(YooAssets), message);
        }
    }
}
