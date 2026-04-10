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
        private readonly bool CoreEnabled = true;  // 必定启用核心
        [SerializeField] private bool NetworkEnabled = true;
        [SerializeField] private bool ProcedureEnabled = true;
        [SerializeField] private bool StorageEnabled = true;

        private void Awake()
        {
            if (CoreEnabled)
            {
                CoreLocator.InfuseCore(new RandomCore(new NormalRNG(Environment.TickCount)));
                CoreLocator.InfuseCore(new LogCore(new UnityLogger()));
                CoreLocator.InfuseCore(new PoolCore());
                CoreLocator.InfuseCore(new EventCore());
            }

            if (NetworkEnabled)
            {
                CoreLocator.InfuseCore(new HttpCore());
                CoreLocator.InfuseCore(new FTPCore());
                CoreLocator.InfuseCore(new ClientCore());
                // CoreLocator.InfuseCore(new ServerCore());
            }

            if (ProcedureEnabled)
            {
                CoreLocator.InfuseCore(new TimeCore(0.05f, 512, 512, 16, 128));
                CoreLocator.InfuseCore(new SceneCore());
            }

            if (StorageEnabled)
            {
                CoreLocator.InfuseCore(new PrefsCore());
                CoreLocator.InfuseCore(new SaveCore(new JsonSerializer()));  // new ProtobufSerializer()
                CoreLocator.InfuseCore(new ResCore(new ResourcesResLoader()));  // new AssetBundleResLoader()
                
                CoreLocator.InfuseCore(new AudioCore());
                CoreLocator.InfuseCore(new LocalizationCore());
            }
            

            CoreLocator.Log.Success(nameof(GoveKitsManager), "GoveKits initialized.");
        }


        private void Update()
        {
            if (ProcedureEnabled)
            {
                CoreLocator.Time.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
            }

            if (StorageEnabled)
            {
                CoreLocator.Res.Update();
            }
        }


        protected override void OnDestroy()
        {
            CoreLocator.Log.Success(nameof(GoveKitsManager), "GoveKits shutdown ing.");

            CoreLocator.Clear();
            base.OnDestroy();
        }
    }
}
