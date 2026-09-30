using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 界面面板基类。一个界面 = 一个面板（如登录界面），绑定一个 ViewModel，UI 组件与 UIItem 由 UIElements 收集。
    ///
    /// 层级：ViewPanel（界面，挂 UIElements）→ UIItem（小组件，自挂 UIElements）
    ///
    /// 用法：
    /// 1. 继承本类并实现自己的面板（或继承 ViewPanel&lt;TVM&gt; 自动绑定 ViewModel）
    /// 2. 挂到界面 GameObject 上（RequireComponent 自动添加 UIElements）
    /// 3. 重写 OnNotify 处理数据刷新、重写生命周期方法处理显示/隐藏
    ///
    /// 组件访问：Elements.Buttons["LoginBtn"]、Elements.ButtonClicked 等
    /// Item 访问：Elements.GetItem&lt;T&gt;("UserNameInput")
    /// 数据访问：VM（仅泛型版）
    /// </summary>
    [RequireComponent(typeof(UIElements))]
    public abstract class ViewPanel : MonoBehaviour
    {
        /// <summary>组件收集器（收集本界面的 UI 组件与子级 UIItem）。Inspector 自动填充，无需手动赋值。</summary>
        [SerializeField] protected UIElements Elements;

        /// <summary>确保 Elements 引用有效（Inspector 未赋值时自动获取）。</summary>
        protected virtual void Awake()
        {
            if (Elements == null) Elements = GetComponent<UIElements>();
        }

        /// <summary>销毁时自动注销注册（按实例身份校验，不会误注销同类型的新实例）。重写时请调用 base.OnDestroy()。</summary>
        protected virtual void OnDestroy()
        {
            UICore.Unregister(GetType(), this);
        }

        /// <summary>重置 VM 缓存（由 UICore.Close 调用），确保 Close 后不会访问到孤儿 VM。</summary>
        internal virtual void ResetVMCache() { }

        /// <summary>
        /// 数据刷新回调。ViewModel 数据变化（Notify）时被调用。
        /// 重写后根据 key 分支刷新对应控件；key 为 null 表示全量刷新（界面绑定 VM 时自动触发一次）。
        /// </summary>
        public virtual void OnNotify(string key) { }

        /// <summary>显示时接收外部传入参数（UICore.Show 的 param）。重写以接收参数，如 Show&lt;LoginPanel&gt;(userData)。</summary>
        public virtual void OnReceiveShowParam(object param) { }

        /// <summary>界面已激活、VM 已绑定后回调。重写以播放入场动画、初始化展示等。</summary>
        public virtual void OnShow() { }

        /// <summary>界面失活前回调。重写以播放退场动画等。</summary>
        public virtual void OnHide() { }
    }

    /// <summary>
    /// 泛型界面面板基类，自动绑定指定类型的 ViewModel。
    /// 界面激活时自动绑定 VM 并全量刷新，失活时自动解绑，无需手动管理。
    ///
    /// 用法：
    /// public class LoginPanel : ViewPanel&lt;LoginVM&gt; { ... }
    /// 界面内通过 VM 属性读写数据；界面外通过 UICore.GetVM&lt;LoginVM&gt;() 访问同一实例。
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型（与界面一一对应）</typeparam>
    public abstract class ViewPanel<TVM> : ViewPanel where TVM : ViewModel, new()
    {
        private TVM _vm;

        /// <summary>本界面绑定的 ViewModel 实例（由 UICore 按类型持有，外部可通过 UICore.GetVM 访问同一实例）。</summary>
        public TVM VM => _vm ??= UICore.GetVM<TVM>();

        /// <summary>重置 VM 缓存（由 UICore.Close 调用），Close 后 VM 属性会重新向 UICore 获取。</summary>
        internal override void ResetVMCache() => _vm = null;

        /// <summary>激活时绑定 VM（触发一次全量刷新），由 UICore.Show 自动驱动。</summary>
        protected virtual void OnEnable()
        {
            VM.AttachView(this);
        }

        /// <summary>失活时解绑 VM，由 UICore.Hide 自动驱动。仅解绑已持有的 VM，禁止经惰性 getter 新建孤儿 VM。</summary>
        protected virtual void OnDisable()
        {
            if (_vm != null) _vm.DetachView(this);
        }
    }
}
