using System.Collections.Generic;


namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// MVVM 模式中的 ViewModel 基类
    /// </summary>
    public abstract class ViewModel
    {
        protected readonly List<ViewPanel> views = new List<ViewPanel>();

        public virtual void OnInit()
        {
            // 初始化数据
        }

        /// <summary>
        /// 绑定 ViewModel 和 View 的关系
        /// 在 ViewModel 中维护一个 View 列表，支持多 View 绑定同一 ViewModel
        /// </summary>
        public void BindView(ViewPanel view)
        {
            if (!views.Contains(view))
            {
                views.Add(view);
            }
        }

        /// <summary>
        /// 解绑 ViewModel 和 View 的关系
        /// 当 View 销毁或不再需要更新时调用，避免内存泄漏
        /// </summary>
        public void UnbindView(ViewPanel view)
        {
            if (views.Contains(view))
            {
                views.Remove(view);
            }
        }

        /// <summary>
        /// 通知所有绑定的 View 更新
        /// </summary>
        /// <param name="key">属性名称</param>
        protected void NotifyViews(string key)
        {
            // 倒序遍历，防止在更新过程中 View 卸载（调用 UnbindView）导致集合修改异常
            for (int i = views.Count - 1; i >= 0; i--)
            {
                views[i].OnNotify(key);
            }
        }
    }

}