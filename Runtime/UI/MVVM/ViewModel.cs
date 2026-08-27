namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// MVVM 数据模型基类（ViewModel）。与 ViewPanel 一一对应，由 UICore 按类型持有。
    /// 负责界面数据与业务逻辑，数据变化时通过 Notify(key) 通知界面刷新。
    ///
    /// 用法：
    /// 1. 继承本类，声明界面数据与操作方法
    /// 2. 数据变化处调用 Notify(key)（key 传 null 表示全量刷新）
    /// 3. 界面内通过 ViewPanel&lt;TVM&gt;.VM 访问；界面外通过 UICore.GetVM&lt;TVM&gt;() 访问同一实例
    ///
    /// 约定：key 传 null 表示全量刷新（界面绑定 VM 时自动触发一次）。
    /// </summary>
    public abstract class ViewModel
    {
        /// <summary>绑定的界面（一个 ViewModel 仅对应一个 ViewPanel）。</summary>
        private ViewPanel _view;

        /// <summary>
        /// 初始化回调。VM 首次被 UICore.GetVM 创建时自动调用一次（与界面类型一一对应）。
        /// 重写以初始化数据，如读取存档、预加载配置。
        /// </summary>
        public virtual void OnInit()
        {
            // 子类可在此初始化数据
        }

        /// <summary>
        /// 绑定界面。界面激活时由 ViewPanel 自动调用，绑定后立即推送一次全量刷新。
        /// 通常无需手动调用。
        /// </summary>
        public void AttachView(ViewPanel view)
        {
            _view = view;
            view.OnNotify(null);
        }

        /// <summary>
        /// 解绑界面。界面失活时由 ViewPanel 自动调用，通常无需手动调用。
        /// </summary>
        public void DetachView(ViewPanel view)
        {
            if (_view == view) _view = null;
        }

        /// <summary>
        /// 通知绑定的界面刷新数据。
        /// </summary>
        /// <param name="key">数据变化标识（如 "gold"），界面 OnNotify 据此分支刷新；传 null 表示全量刷新</param>
        protected void Notify(string key)
        {
            _view?.OnNotify(key);
        }
    }
}
