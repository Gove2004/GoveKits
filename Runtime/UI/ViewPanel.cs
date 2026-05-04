using UnityEngine;
using System.ComponentModel;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 面板基类（非泛型）
    /// 
    /// 核心功能：
    /// 1. 继承 UIElementCollection，自动绑定 UI 组件
    /// 2. 提供面板生命周期方法（OnShow/OnHide）
    /// 3. 支持入口面板和弹窗标记
    /// 4. 持有 UIController 引用用于导航
    /// </summary>
    public abstract class ViewPanel : UIElementCollection
    {
        [Header("UI 属性")]
        /// <summary>是否为入口界面 - 初始化时自动显示</summary>
        public bool isEntry = false;
        /// <summary>是否为弹窗 - 弹窗不进入导航栈</summary>
        public bool isPopup = false;

        /// <summary>UI 控制器引用 - 用于页面导航</summary>
        protected UIController Controller;
        
        /// <summary>设置控制器引用（由 UIController 初始化时调用）</summary>
        public void SetUIController(UIController controller) => Controller = controller;

        /// <summary>
        /// 面板显示时调用
        /// 默认实现：激活 GameObject
        /// </summary>
        /// <param name="payload">传递的数据参数</param>
        public virtual void OnShow(object payload = null) => gameObject.SetActive(true);
        
        /// <summary>
        /// 面板隐藏时调用
        /// 默认实现：禁用 GameObject
        /// </summary>
        public virtual void OnHide() => gameObject.SetActive(false);
    }

    /// <summary>
    /// UI 面板基类（泛型版本 - 支持 MVVM 绑定）
    /// 
    /// 核心功能：
    /// 1. 自动关联指定类型的 ViewModel
    /// 2. 订阅 ViewModel 的 PropertyChanged 事件
    /// 3. 面板显示/隐藏时自动绑定/解绑事件
    /// 4. 显示时触发一次全量刷新
    /// 
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型</typeparam>
    public abstract class ViewPanel<TVM> : ViewPanel where TVM : ViewModel, new()
    {        
        /// <summary>关联的 ViewModel 实例</summary>
        protected TVM ViewModel { get; private set; }
        
        /// <summary>
        /// 数据变更回调 - 子类必须实现
        /// 用于根据 ViewModel 属性变化更新 UI
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">属性变更事件参数</param>
        protected abstract void OnDataChanged(object sender, PropertyChangedEventArgs e);

        /// <summary>
        /// 面板显示 - 绑定 ViewModel 并订阅事件
        /// </summary>
        public override void OnShow(object payload = null)
        {
            // 从 UIController 获取单例 ViewModel
            ViewModel = VMContainer.Get<TVM>();
            // 订阅属性变更事件
            ViewModel.PropertyChanged += OnDataChanged;
            
            base.OnShow(payload);
            
            // 初始触发一次全量刷新 (propertyName 为空)
            // 让子类有机会初始化所有 UI 元素
            OnDataChanged(ViewModel, new PropertyChangedEventArgs(null));
        }

        /// <summary>
        /// 面板隐藏 - 解绑 ViewModel 事件，避免内存泄漏
        /// </summary>
        public override void OnHide()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnDataChanged;
                ViewModel = null;
            }
            base.OnHide();
        }

        
        //
        protected virtual void OnDestroy()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnDataChanged;
                ViewModel = null;
            }
        }
    }
}