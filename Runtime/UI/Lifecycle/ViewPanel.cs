using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 面板视图基类，负责面板生命周期。
    /// 组件收集由独立的 UIElementCollection 负责，通过 Elements 属性组合访问。
    /// 子类继承后重写生命周期方法。
    /// </summary>
    public abstract class ViewPanel : MonoBehaviour
    {
        /// <summary>
        /// 组件收集器。同 GameObject 上的 UIElementCollection 组件，
        /// 子类通过 Elements.Buttons["BtnName"] 等方式访问 UI 组件，通过 Elements.ButtonClicked 等订阅交互事件。
        /// 若无收集需求可忽略。
        /// </summary>
        protected UIElementCollection Elements { get; private set; }

        protected virtual void Awake()
        {
            Elements = GetComponent<UIElementCollection>();
        }

        /// <summary>
        /// ViewModel 通知视图更新时调用。子类根据 key 分支处理不同数据变化。
        /// </summary>
        public abstract void OnNotify(string key);

        /// <summary>绑定 ViewModel，子类重写实现自定义绑定逻辑。</summary>
        public virtual void OnBindVM() { }

        /// <summary>解绑 ViewModel，子类重写实现自定义解绑逻辑。</summary>
        public virtual void OnUnbindVM() { }

        /// <summary>面板显示时接收外部参数，默认空实现，子类可重写。</summary>
        public virtual void OnReceiveShowParam(object param) { }

        /// <summary>面板显示，激活 GameObject。</summary>
        public virtual void OnShow() => gameObject.SetActive(true);

        /// <summary>面板隐藏，禁用 GameObject。</summary>
        public virtual void OnHide() => gameObject.SetActive(false);
    }

    /// <summary>
    /// 泛型 UI 面板基类，自动关联指定类型的 ViewModel。
    /// 面板显示/隐藏时自动绑定/解绑 ViewModel，避免内存泄漏。
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型</typeparam>
    public abstract class ViewPanel<TVM> : ViewPanel where TVM : ViewModel, new()
    {
        /// <summary>关联的 ViewModel 实例，由 OnBindVM 自动赋值。</summary>
        protected TVM VM { get; private set; }

        /// <summary>
        /// 绑定 ViewModel：从 UICore 获取单例并建立双向关联。
        /// 多次调用时自动先解绑再重新绑定。
        /// </summary>
        public override void OnBindVM()
        {
            if (VM != null)
            {
                VM.DetachView(this);
            }
            VM = UICore.GetVM<TVM>();
            VM.AttachView(this);
        }

        /// <summary>
        /// 解绑 ViewModel：断开与 ViewModel 的关联。
        /// </summary>
        public override void OnUnbindVM()
        {
            if (VM != null)
            {
                VM.DetachView(this);
                VM = null;
            }
        }
    }
}
