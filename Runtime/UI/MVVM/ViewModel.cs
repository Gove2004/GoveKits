namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// MVVM 模式中的 ViewModel 基类。
    /// 与 ViewPanel 一一对应（由 UICore 按类型持有），数据变化时通过 Notify(key) 通知绑定的界面。
    ///
    /// 约定：key 传 null 表示全量刷新（界面绑定 VM 时自动触发一次）。
    /// </summary>
    public abstract class ViewModel
    {
        /// <summary>绑定的界面（一个 ViewModel 仅对应一个 ViewPanel）。</summary>
        private ViewPanel _view;

        /// <summary>
        /// 初始化回调，在 VM 首次被 UICore.GetVM 创建时自动调用（与界面类型一一对应，仅一次）。
        /// 子类可重写此方法进行数据初始化。
        /// </summary>
        public virtual void OnInit()
        {
            // 子类可在此初始化数据
        }

        /// <summary>
        /// 绑定界面。绑定后立即推送一次全量刷新（OnNotify(null)），
        /// 使界面按当前数据完成初始渲染，无需手动初始化。
        /// </summary>
        public void AttachView(ViewPanel view)
        {
            _view = view;
            view.OnNotify(null);
        }

        /// <summary>
        /// 解绑界面。当界面失活或销毁时调用，避免内存泄漏。
        /// </summary>
        public void DetachView(ViewPanel view)
        {
            if (_view == view) _view = null;
        }

        /// <summary>
        /// 解除绑定的界面引用，用于清理。
        /// </summary>
        public void DetachAllViews()
        {
            _view = null;
        }

        /// <summary>
        /// 通知绑定的界面更新。
        /// </summary>
        /// <param name="key">更新键值，标识哪个数据发生了变化；传 null 表示全量刷新</param>
        protected void Notify(string key)
        {
            _view?.OnNotify(key);
        }
    }
}
