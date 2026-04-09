using GoveKits.Runtime.Core;
using GoveKits.Runtime.Network;
using System;
using UnityEngine;


namespace GoveKits.Runtime
{
    public class GoveKitsManager : MonoSingleton<GoveKitsManager>
    {
        [SerializeField] private bool CoreEnabled = true;
        [SerializeField] private bool NetworkEnabled = true;

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
            }
            

            CoreLocator.Log.Success(nameof(GoveKitsManager), "GoveKits initialized.");
        }


        private void Update()
        {
            // TimerManager.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
        }


        protected override void OnDestroy()
        {
            CoreLocator.Clear();

            CoreLocator.Log.Success(nameof(GoveKitsManager), "GoveKits shutdown.");
        }
    }
}
