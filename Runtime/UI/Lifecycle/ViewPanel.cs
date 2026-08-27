using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 面板视图基类，负责面板生命周期与数据刷新。
    /// 强制要求同 GameObject 上挂载 UIElements 组件用于收集 UI 组件。
    ///
    /// 生命周期（由 UICore.Show/Hide 驱动，绑定/解绑自动完成）：
    ///   Show: OnReceiveShowParam(param) → 激活(自动绑定 VM + 全量刷新) → OnShow()
    ///   Hide: OnHide() → 失活(自动解绑 VM)
    ///
    /// 数据刷新：VM 数据变化调用 Notify(key)，面板重写 OnNotify(key) 分支更新控件。
    /// key 传 null 表示全量刷新（绑定 VM 时自动触发一次）。
    /// </summary>
    [RequireComponent(typeof(UIElements))]
    public abstract class ViewPanel : MonoBehaviour
    {
        /// <summary>组件收集器，Inspector 自动填充（RequireComponent 保证存在）。</summary>
        [SerializeField] protected UIElements Elements;

        protected virtual void Awake()
        {
            if (Elements == null) Elements = GetComponent<UIElements>();
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        /// <summary>
        /// ViewModel 通知视图更新时调用，子类根据 key 分支处理。
        /// key 为 null 时表示全量刷新（绑定 VM 后自动触发）。
        /// </summary>
        public abstract void OnNotify(string key);

        /// <summary>面板显示时接收外部参数，默认空实现，子类可重写。</summary>
        public virtual void OnReceiveShowParam(object param) { }

        /// <summary>面板显示完成回调（已激活、VM 已绑定），子类可重写播放动画等。</summary>
        public virtual void OnShow() { }

        /// <summary>面板隐藏回调（失活前），子类可重写播放淡出等。</summary>
        public virtual void OnHide() { }
    }

    /// <summary>
    /// 泛型 UI 面板基类，自动关联指定类型的 ViewModel。
    /// 激活时自动绑定 VM 并全量刷新，失活时自动解绑，无需手动管理。
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型</typeparam>
    public abstract class ViewPanel<TVM> : ViewPanel where TVM : ViewModel, new()
    {
        /// <summary>关联的 ViewModel 实例，激活时自动绑定。</summary>
        protected TVM VM { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            VM = UICore.GetVM<TVM>();
            VM.AttachView(this);    // 绑定即全量刷新（触发 OnNotify(null)）
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            VM?.DetachView(this);
        }
    }
}
