using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Network;
using GoveKits.Runtime.Procedure;
using GoveKits.Runtime.Storage;
using MessagePack.Resolvers;
using System;
using YooAsset;

namespace GoveKits.Runtime
{
    public class GoveKitsManager : MonoSingleton<GoveKitsManager>
    {
    
        #region 生命周期

        private void Awake()
        {
            Initialize();
        }
        private async void Initialize()
        {
            // 依赖框架
            YooAssets.Initialize(new YooLogger());

            // Core
            LogCore.InfuseLogger(new UnityLogger());
            RandomCore.Initialize(new NormalRNG(Environment.TickCount));

            // Procedure
            TimeCore.Initialize(0.05f, 512, 512, 16, 128);

            // Network
            // ProtocolCore.SetResolver(GeneratedResolver.Instance);
            ProtocolCore.ScanProtocols();

            // Storage
            SaveCore.Initialize(new JsonSerializer());
            // await ResCore.PackageWorkflowAsync(new AutoOfflinePackageConfig("DefaultPackage"), new UpdateCallbacks());
            // await UniTask.WaitForSeconds(30f);  // ConfigCore 依赖 ResCore
            // ConfigCore.InfuseParser(new JsonConfigParser());
            // ConfigCore.InfuseParser(new CsvConfigParser());
            // ConfigCore.Initialize();
            // await UniTask.Yield();  // LocalizationCore 依赖 ConfigCore
            // LocalizationCore.Initialize();
            // AudioCore.Initialize(16);

            // 
            LogCore.Success(nameof(GoveKitsManager), "GoveKits initialized.");
        }


        private void Update()
        {
            TimeCore.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
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
            LogCore.Warn(nameof(YooAssets), message);
        }
    }
}
