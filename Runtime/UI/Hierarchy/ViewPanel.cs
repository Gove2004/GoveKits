using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 界面面板基类（ViewPanel 层）。
    /// 一个界面 = 一个面板（如登录界面），绑定一个 ViewModel，包含若干可复用的 UIItem。
    ///
    /// 层级：ViewPanel（界面，挂 UIElements）→ UIItem（小组件，自挂 UIElements）
    ///
    /// 生命周期（由 UICore.Show/Hide 驱动）：
    ///   Show: OnReceiveShowParam(param) → 激活(自动绑定 VM + 全量刷新) → OnShow()
    ///   Hide: OnHide() → 失活(自动解绑 VM)
    ///
    /// 组件访问：Elements.Buttons["BtnName"]、Elements.ButtonClicked 等
    /// Item 访问：GetItem&lt;T&gt;("UserNameInput")
    /// 数据访问：VM（泛型版直接持有）
    /// </summary>
    [RequireComponent(typeof(UIElements))]
    public abstract class ViewPanel : MonoBehaviour
    {
        /// <summary>组件收集器，Inspector 自动填充（RequireComponent 保证存在）。</summary>
        [SerializeField] protected UIElements Elements;

        /// <summary>子级 Item 字典 - 按名称索引（Awake 自动收集）。</summary>
        protected readonly Dictionary<string, UIItem> _items = new();

        /// <summary>本界面绑定的 ViewModel（由 UICore 按类型持有，与界面一一对应），未绑定返回 null。</summary>
        public abstract ViewModel GetVM();

        protected virtual void Awake()
        {
            if (Elements == null) Elements = GetComponent<UIElements>();

            foreach (var item in GetComponentsInChildren<UIItem>(true))
            {
                _items.TryAdd(item.name, item);
            }
        }

        /// <summary>按名称获取 Item，未注册返回 null。</summary>
        protected UIItem GetItem(string name)
            => _items.TryGetValue(name, out var item) ? item : null;

        /// <summary>按名称获取指定类型的 Item。</summary>
        protected T GetItem<T>(string name) where T : UIItem
            => GetItem(name) as T;

        /// <summary>VM 通知入口，子类按 key 刷新界面。key 传 null 表示全量刷新。</summary>
        public virtual void OnNotify(string key) { }

        /// <summary>界面显示时接收外部参数，默认空实现，子类可重写。</summary>
        public virtual void OnReceiveShowParam(object param) { }

        /// <summary>界面显示完成回调（已激活、VM 已绑定），子类可重写播放动画等。</summary>
        public virtual void OnShow() { }

        /// <summary>界面隐藏回调（失活前），子类可重写播放淡出等。</summary>
        public virtual void OnHide() { }
    }

    /// <summary>
    /// 泛型界面基类，与指定类型的 ViewModel 一一对应。
    /// VM 实例由 UICore 按类型持有（外部可通过 UICore.GetVM 访问同一实例），
    /// 激活时自动绑定并全量刷新，失活时自动解绑。
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型</typeparam>
    public abstract class ViewPanel<TVM> : ViewPanel where TVM : ViewModel, new()
    {
        private TVM _vm;

        /// <summary>本界面绑定的 ViewModel 实例（由 UICore 持有，与界面类型一一对应）。</summary>
        public TVM VM => _vm ??= UICore.GetVM<TVM>();

        public override ViewModel GetVM() => VM;

        protected virtual void OnEnable()
        {
            VM.AttachView(this);    // 绑定即全量刷新（触发 OnNotify(null)）
        }

        protected virtual void OnDisable()
        {
            VM?.DetachView(this);
        }
    }
}
