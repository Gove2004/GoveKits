using Cysharp.Threading.Tasks;
using DG.Tweening;
using GoveKits.Events;
using UnityEngine;

namespace GoveKits.UI
{
    /// <summary>
    /// UI 面板的抽象基类，基于安卓 Activity 生命周期设计。
    /// 提供了完整的生命周期方法和默认的淡入淡出动画实现。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePanel : MonoBehaviour, IPanelLifeCycle
    {
        [Tooltip("是否为入口面板，第一个显示的界面")]
        public bool isEntry = false;
        [Tooltip("是否为弹窗（弹窗打开时，不会隐藏其下方的面板）")]
        public bool isPopup = false;
        
        
        /// <summary>
        /// 面板是否已被创建（OnCreate 已被调用）。
        /// </summary>
        public bool IsCreated { get; private set; } = false;

        protected UIController uiController;
        private CanvasGroup canvasGroup;

        /// <summary>
        /// 对面板 CanvasGroup 组件的缓存访问器。
        /// </summary>
        public CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
                return canvasGroup;
            }
        }

        /// <summary>
        /// 由 UIController 在初始化时调用，用于注入控制器引用。
        /// </summary>
        public void SetUIController(UIController controller) => uiController = controller;


        #region 显式接口实现 (封装生命周期调用)

        // 通过显式接口实现，这些生命周期方法无法被外部直接调用，
        // 只能通过 IPanelLifeCycle 接口被 UIController 驱动，保证了状态管理的唯一性和安全性。

        void IPanelLifeCycle.OnCreate() => OnCreate();
        void IPanelLifeCycle.OnStart(object payload) => OnStart(payload);
        
        // 将异步方法连接到接口
        void IPanelLifeCycle.OnResume() => OnResume();
        void IPanelLifeCycle.OnPause() => OnPause();
        void IPanelLifeCycle.OnStop() => OnStop();
        
        void IPanelLifeCycle.OnFinish() => OnFinish();

        #endregion

        #region 子类可重写的生命周期方法

        /// <summary>
        /// 【生命周期】首次创建时调用，只会调用一次。
        /// 适合进行获取组件、初始化列表等一次性操作。
        /// </summary>
        protected virtual void OnCreate()
        {
            EventManager.Publish<PanelEvent>(
                (pe) => pe.SetData(PanelLifeType.OnCreate, this)
            );
            IsCreated = true;
        }

        /// <summary>
        /// 【生命周期】面板被激活并显示前调用。
        /// 适合接收参数、设置初始UI状态。
        /// </summary>
        /// <param name="payload">由 UIController.Show() 传入的参数。</param>
        protected virtual void OnStart(object payload = null)
        {
            EventManager.Publish<PanelEvent>(
                (pe) => pe.SetData(PanelLifeType.OnStart, this)
            );
            this.gameObject.SetActive(true);
            this.transform.SetAsLastSibling(); // 确保新打开的面板在最上层
        }

        /// <summary>
        /// 【异步生命周期】面板进入前台，变为完全可交互状态时调用。
        /// 默认实现了一个淡入动画。
        /// </summary>
        public virtual void OnResume()
        {
            EventManager.Publish<PanelEvent>(
                (pe) => pe.SetData(PanelLifeType.OnResume, this)
            );
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.DOFade(1, 0.3f);
        }

        /// <summary>
        /// 【异步生命周期】面板被部分遮挡（如被弹窗覆盖），变为不可交互状态时调用。
        /// 默认实现了一个轻微淡出的动画。
        /// </summary>
        public virtual void OnPause()
        {
            EventManager.Publish<PanelEvent>(
                (pe) => pe.SetData(PanelLifeType.OnPause, this)
            );
            CanvasGroup.interactable = false;
            // 可以选择在这里播放一个轻微变暗的动画
            CanvasGroup.DOFade(0.8f, 0.2f);
        }

        /// <summary>
        /// 【异步生命周期】面板完全从视野中消失时调用。
        /// 默认实现了一个完全淡出的动画，并在动画结束后禁用 GameObject。
        /// </summary>
        public virtual void OnStop()
        {
            EventManager.Publish<PanelEvent>(
                (pe) => pe.SetData(PanelLifeType.OnStop, this)
            );
            CanvasGroup.blocksRaycasts = false; // 在动画期间就禁止交互
            CanvasGroup.DOFade(0, 0.3f).onComplete += () =>
            {
                this.gameObject.SetActive(false);
            };
           
        }

        /// <summary>
        /// 【生命周期】面板被销毁前调用。
        /// 适合进行资源释放、事件解绑等清理工作。
        /// </summary>
        protected virtual void OnFinish()
        {
            EventManager.Publish<PanelEvent>(
                (pe) => pe.SetData(PanelLifeType.OnFinish, this)
            );
            IsCreated = false;
            // 如果与对象池结合，可以在这里调用 Pool.Recycle(this)
            Destroy(this.gameObject);
        }

        #endregion
    }
}