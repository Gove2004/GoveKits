using System;
using System.Collections.Generic;

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
        /// <summary>绑定的视图列表，通过倒序遍历防止集合修改异常。</summary>
        protected readonly List<ViewPanel> _views = new List<ViewPanel>();

        /// <summary>
        /// 初始化回调，在 VM 首次被 UICore.GetVM 创建时自动调用（与界面类型一一对应，仅一次）。
        /// 子类可重写此方法进行数据初始化。
        /// </summary>
        public virtual void OnInit()
        {
            // 子类可在此初始化数据
        }

        /// <summary>
        /// 绑定视图。绑定后立即推送一次全量刷新（OnNotify(null)），
        /// 使视图按当前数据完成初始渲染，无需手动初始化。
        /// </summary>
        public void AttachView(ViewPanel view)
        {
            if (!_views.Contains(view))
            {
                _views.Add(view);
                view.OnNotify(null);
            }
        }

        /// <summary>
        /// 解绑视图。当视图失活或销毁时调用，避免内存泄漏。
        /// </summary>
        public void DetachView(ViewPanel view)
        {
            _views.Remove(view);
        }

        /// <summary>
        /// 解除所有绑定的视图引用，用于清理。
        /// </summary>
        public void DetachAllViews()
        {
            _views.Clear();
        }

        /// <summary>
        /// 通知所有绑定的视图更新。
        /// 倒序遍历，防止在更新过程中视图卸载导致集合修改异常。
        /// </summary>
        /// <param name="key">更新键值，标识哪个数据发生了变化；传 null 表示全量刷新</param>
        protected void Notify(string key)
        {
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnNotify(key);
            }
        }
    }
}
