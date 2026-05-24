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
    /// 3. 持有 UIController 引用用于导航
    /// </summary>
    public abstract class ViewPanel : UIElementCollection
    {
        public abstract void OnNotify(string paramKey);  // ViewModel 通知 View 更新时调用
        public abstract void OnBindVM();  // 绑定 ViewModel
        public abstract void OnUnbindVM();  // 解绑 ViewModel

        public abstract void OnReceiveShowParam(object param);  // 弹窗参数初始化
        public abstract void OnShow();  // 显示时
        public abstract void OnHide();  // 隐藏时
        // public abstract void OnOldToDrop();  // 静默销毁
    }

    /// <summary>
    /// UI 面板基类
    /// 
    /// 核心功能：
    /// 1. 自动关联指定类型的 ViewModel
    /// 2. 面板显示/隐藏时自动绑定/解绑 ViewModel
    /// 3. 避免内存泄漏
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型</typeparam>
    public abstract class ViewPanel<TVM> : ViewPanel where TVM : ViewModel, new()
    {        
        /// <summary>关联的 ViewModel 实例</summary>
        protected TVM VM { get; private set; }

        public override void OnBindVM()
        {
            VM = UICore.GetVM<TVM>();
            VM.BindView(this);
        }

        public override void OnUnbindVM()
        {
            if (VM != null)
            {
                VM.UnbindView(this);
                VM = null;
            }
        }

        public override void OnReceiveShowParam(object param) { }
        public override void OnShow() => this.gameObject.SetActive(true);
        public override void OnHide() => this.gameObject.SetActive(false);
        // public override void OnOldToDrop() => GameObject.Destroy(this.gameObject);
    }
}