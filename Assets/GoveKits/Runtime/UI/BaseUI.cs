using UnityEngine;

// 类似 安卓 Activity 的生命周期管理

namespace GoveKits.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PanelUI : MonoBehaviour
    {
        [Header("UI Configuration")]
        public bool isEntry = false;
        public bool isPopup = false; 
        
        public bool IsCreated { get; private set; } = false;
        
        // 参数存储
        protected object Parameters;
        
        protected UIController uiController;
        public void SetUIController(UIController controller) => uiController = controller;
        
        private CanvasGroup canvasGroup;
        public CanvasGroup CanvasGroup 
        {
            get 
            {
                if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
                return canvasGroup;
            }
        }

        #region 内部生命周期方法（仅供Controller调用）

        internal void InternalOnCreate()
        {
            IsCreated = true;
            OnCreate();
        }

        internal void InternalOnStart(object parameters)
        {
            this.Parameters = parameters;
            this.gameObject.SetActive(true);
            this.transform.SetAsLastSibling();
            
            OnStart();
        }

        internal void InternalOnResume()
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            OnResume();
        }

        internal void InternalOnPause()
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = true;
            OnPause();
        }

        internal void InternalOnStop()
        {
            this.gameObject.SetActive(false);
            OnStop();
        }

        #endregion

        #region 子类可重写的受保护方法

        /// <summary>
        /// 界面创建时调用（只执行一次）
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// 界面显示时调用，可在这里处理参数
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// 界面获取焦点时调用
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 界面失去焦点时调用
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 界面隐藏时调用
        /// </summary>
        protected virtual void OnStop() { }

        #endregion
    }
}