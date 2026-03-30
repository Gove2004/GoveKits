using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Util;
using UnityEngine;
using System;

namespace GoveKits.Runtime
{
     /// <summary>
    /// GoveKitsCore 的生命周期组件，负责在适当的时机调用
    /// GoveKitsCore.Initialize() 和 GoveKitsCore.Shutdown()。
    /// </summary>
    public class GoveKitsManager : MonoBehaviour
    {
        private void Awake()
        {
            RandomCore.Init(Environment.TickCount);  // 使用系统时间戳初始化随机数生成器。
#if UNITY_EDITOR
            LogCore.InfuseLogger(new UnityLogger());  // 默认注入 Unity 日志器
#endif

            // TimerManager.Initialize();
            // ConfigCore.InitAsync().Forget();  // 异步初始化配置系统，不等待完成。

            // 目前核心模块没有需要初始化的内容，但可以在这里添加全局设置或预热逻辑。
            LogCore.Success(nameof(GoveKitsManager), "GoveKits initialized.");
        }

        private void Update()
        {
            // TimerManager.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            LogCore.Success(nameof(GoveKitsManager), "GoveKits shutdown.");
        }
    }        
}