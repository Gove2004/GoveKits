using UnityEngine;



///
/// 基于安卓 Activity 生命周期设计的 UI 面板基类
/// 包含 OnCreate, OnStart, OnResume, OnPause, OnStop, OnFinish 六个生命周期方法
/// 通过 UIController 来驱动这些生命周期方法
/// 
namespace GoveKits.UI
{
    // 这个接口只在 UIController 内部使用，用来驱动生命周期
    public interface IUILifeCycle
    {
        void OnCreate();
        void OnStart(object payload = null);  // 增加 payload 参数
        void OnResume();
        void OnPause();
        void OnStop();
        void OnFinish();
    }
}





namespace GoveKits.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    // 实现 IUILifeCycle 接口
    public abstract class PanelUI : MonoBehaviour, IUILifeCycle
    {
        public bool isEntry = false;  // 是否为入口面板
        public bool isPopup = false;  // 是否为弹窗（弹窗不隐藏下层面板）
        public bool IsCreated { get; private set; } = false;  // 是否已创建
        
        protected UIController uiController;
        public void SetUIController(UIController controller) => uiController = controller;
        private CanvasGroup canvasGroup;
        
        // 供子类访问的 CanvasGroup
        public CanvasGroup CanvasGroup 
        {
            get 
            {
                if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
                return canvasGroup;
            }
        }


        #region 显式接口实现 (外界点不出来，只有 Controller 能调)

        void IUILifeCycle.OnCreate() => OnCreate();
        
        // 核心改动：接收参数并转发给 protected 方法
        void IUILifeCycle.OnStart(object payload) => OnStart(payload);
        
        void IUILifeCycle.OnResume() => OnResume();
        void IUILifeCycle.OnPause() => OnPause();
        void IUILifeCycle.OnStop() => OnStop();
        void IUILifeCycle.OnFinish() => OnFinish();

        #endregion

        #region 子类可重写的逻辑 (Protected)

        // 子类只能 override 这些 protected 方法
        // 别的脚本拿到 PanelUI 实例也无法调用这些方法，只有 Controller 能通过接口触发

        protected virtual void OnCreate() 
        {
            IsCreated = true;
        }

        /// <summary>
        /// 界面显示。
        /// <param name="payload">传入的参数 (如弹窗的标题、回调等)</param>
        /// </summary>
        protected virtual void OnStart(object payload) 
        {
            this.gameObject.SetActive(true);
            this.transform.SetAsLastSibling();
        }

        protected virtual void OnResume() 
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        protected virtual void OnPause() 
        {
            CanvasGroup.interactable = false;
        }

        protected virtual void OnStop() 
        {
            this.gameObject.SetActive(false);
        }

        protected virtual void OnFinish() 
        {
            IsCreated = false;
        }

        #endregion
    }
}