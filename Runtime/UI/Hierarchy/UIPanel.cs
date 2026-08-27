using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 面板基类（Panel 层）。
    /// 一个面板 = 界面中的一个功能区（如登录面板、注册面板）。
    /// 挂载 UIElements 收集本面板的 UI 组件，并自动检测注册子级 UIWidget。
    ///
    /// 层级：UIView（界面）→ UIPanel（面板）→ UIWidget（小组件）
    ///
    /// 组件访问：Elements.Buttons["BtnName"]、Elements.ButtonClicked 等
    /// Widget 访问：GetWidget&lt;T&gt;("UserNameInput")
    /// 数据访问：GetVM&lt;T&gt;() 从父级 UIView 获取 UIViewModel
    /// </summary>
    [RequireComponent(typeof(UIElements))]
    public abstract class UIPanel : MonoBehaviour
    {
        /// <summary>组件收集器，Inspector 自动填充（RequireComponent 保证存在）。</summary>
        [SerializeField] protected UIElements Elements;

        /// <summary>父级界面，Awake 时获取。</summary>
        public UIView ParentView { get; private set; }

        /// <summary>子级 Widget 字典 - 按名称索引（Awake 自动收集）。</summary>
        protected readonly Dictionary<string, UIWidget> _widgets = new();

        protected virtual void Awake()
        {
            if (Elements == null) Elements = GetComponent<UIElements>();
            ParentView = GetComponentInParent<UIView>();

            foreach (var widget in GetComponentsInChildren<UIWidget>(true))
            {
                _widgets.TryAdd(widget.name, widget);
            }
        }

        /// <summary>按名称获取 Widget，未注册返回 null。</summary>
        protected UIWidget GetWidget(string name)
            => _widgets.TryGetValue(name, out var w) ? w : null;

        /// <summary>按名称获取指定类型的 Widget。</summary>
        protected T GetWidget<T>(string name) where T : UIWidget
            => GetWidget(name) as T;

        /// <summary>从父级界面获取 UIViewModel。</summary>
        protected T GetVM<T>() where T : UIViewModel
            => ParentView?.GetVM() as T;

        /// <summary>界面转发来的 VM 通知，子类按 key 刷新本面板。key 传 null 表示全量刷新。</summary>
        public virtual void OnNotify(string key) { }

        /// <summary>面板显示时接收外部参数，默认空实现，子类可重写。</summary>
        public virtual void OnReceiveShowParam(object param) { }

        /// <summary>面板显示完成回调（已激活），子类可重写。</summary>
        public virtual void OnShow() { }

        /// <summary>面板隐藏回调（失活前），子类可重写。</summary>
        public virtual void OnHide() { }
    }
}
