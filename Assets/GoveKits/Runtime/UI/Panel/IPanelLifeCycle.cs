using Cysharp.Threading.Tasks;

namespace GoveKits.UI
{
    /// <summary>
    /// 【内部接口】定义了 UI 面板的生命周期。
    /// UIController 通过此接口来驱动面板的状态转换，实现对面板的完全控制。
    /// </summary>
    internal interface IPanelLifeCycle
    {
        // --- 同步生命周期 ---
        void OnCreate();
        void OnStart(object payload = null);
        void OnResume();

        void OnPause();
        void OnStop();

        void OnFinish();
    }

    public enum PanelLifeType
    {
        None,
        OnCreate,
        OnStart,
        OnResume,
        OnPause,
        OnStop,
        OnFinish
    }


}