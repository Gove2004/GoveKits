using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Network;
using GoveKits.Runtime.Procedure;
using GoveKits.Runtime.Storage;
using System;
using UnityEngine;


namespace GoveKits.Runtime
{
    public class GoveKitsManager : MonoSingleton<GoveKitsManager>
    {
        #region 生命周期

        private void Awake()
        {
            // Core
            CoreLocator.InfuseCore(new RandomCore(new NormalRNG(Environment.TickCount)));
            CoreLocator.InfuseCore(new LogCore(new UnityLogger()));
            CoreLocator.InfuseCore(new PoolCore());
            CoreLocator.InfuseCore(new EventCore());

            // Procedure
            CoreLocator.InfuseCore(new TimeCore(0.05f, 512, 512, 16, 128));
            CoreLocator.InfuseCore(new SceneCore());

            // Network
            CoreLocator.InfuseCore(new HttpCore());
            CoreLocator.InfuseCore(new FTPCore());
            CoreLocator.InfuseCore(new ProtocolCore(new StandardMessagePackSerializer()));
            CoreLocator.InfuseCore(new DispatcherCore());
            CoreLocator.InfuseCore(new ClientCore());
            CoreLocator.InfuseCore(new ServerCore());

            // Storage
            CoreLocator.InfuseCore(new PrefsCore());
            CoreLocator.InfuseCore(new SaveCore(new JsonSerializer()));  // new ProtobufSerializer()
            CoreLocator.InfuseCore(new ResCore(new ResourcesResLoader()));  // new AssetBundleResLoader()
            CoreLocator.InfuseCore(new ConfigCore(new IConfigParser[] { new JsonConfigParser(),  new CsvConfigParser() }));
            
            CoreLocator.InfuseCore(new AudioCore());
            CoreLocator.InfuseCore(new LocalizationCore());

            // 在外部覆盖注入新的核心进行覆盖
            // CoreLocator.InfuseCore(new LogCore(new FileLogger()));
            
            // 
            CoreLocator.Log.Success(nameof(GoveKitsManager), "GoveKits initialized.");
        }


        private void Update()
        {
            CoreLocator.Time.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
            CoreLocator.Res.Update();
        }


        protected override void OnDestroy()
        {
            CoreLocator.Log.Success(nameof(GoveKitsManager), "GoveKits shutdown ing.");

            CoreLocator.Clear();
            base.OnDestroy();
        }

        #endregion















    }
}
