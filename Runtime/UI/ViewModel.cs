using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// MVVM 模式中的 ViewModel 基类。
    /// 维护对多个 ViewPanel 的引用，支持通过 key 通知所有绑定的视图更新。
    /// </summary>
    public abstract class ViewModel
    {
        /// <summary>绑定的视图列表，通过倒序遍历防止集合修改异常。</summary>
        protected readonly List<ViewPanel> _views = new List<ViewPanel>();

        /// <summary>
        /// 初始化回调，在 ViewModel 创建后由 UICore.GetVM 自动调用。
        /// 子类可重写此方法进行数据初始化。
        /// </summary>
        public virtual void OnInit()
        {
            // 子类可在此初始化数据
        }

        /// <summary>
        /// 绑定视图。在 ViewModel 中维护一个视图列表，支持多视图绑定同一 ViewModel。
        /// </summary>
        public void AttachView(ViewPanel view)
        {
            if (!_views.Contains(view))
            {
                _views.Add(view);
            }
        }

        /// <summary>
        /// 解绑所有视图。当视图销毁或不再需要更新时调用，避免内存泄漏。
        /// </summary>
        public void DetachView(ViewPanel view)
        {
            _views.Remove(view);
        }

        /// <summary>
        /// 解除所有绑定的视图引用，用于 Close 时清理。
        /// </summary>
        public void DetachAllViews()
        {
            _views.Clear();
        }

        /// <summary>
        /// 通知所有绑定的视图更新。
        /// 倒序遍历，防止在更新过程中视图卸载导致集合修改异常。
        /// </summary>
        /// <param name="key">更新键值，标识哪个数据发生了变化</param>
        protected void NotifyViews(string key)
        {
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].OnNotify(key);
            }
        }
    }
}
