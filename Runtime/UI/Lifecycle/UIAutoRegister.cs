using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 界面自动注册器。挂在场景中任意 GameObject 上，
    /// 自动扫描并注册所有子物体上的 ViewPanel 到 UICore，销毁时自动注销。
    /// 运行时动态 Instantiate 的界面不会经过 Awake 扫描，需手动调用 <see cref="RegisterView"/> 注册。
    ///
    /// 用法：场景根物体挂一个本组件，之后即可通过 UICore.Show&lt;T&gt;() 打开任意注册过的界面，无需手动注册。
    /// </summary>
    public class UIAutoRegister : MonoBehaviour
    {
        private readonly List<ViewPanel> _views = new();

        private void Awake()
        {
            foreach (var view in GetComponentsInChildren<ViewPanel>(true))
                RegisterView(view);
        }

        /// <summary>
        /// 注册一个 ViewPanel 实例（用于运行时动态 Instantiate 的界面，Awake 扫描覆盖不到）。
        /// 重复注册同一实例会被忽略。
        /// </summary>
        public void RegisterView(ViewPanel view)
        {
            if (view == null) return;
            if (_views.Contains(view)) return;

            UICore.Register(view.GetType(), view);
            _views.Add(view);
        }

        /// <summary>
        /// 注销一个 ViewPanel 实例（动态界面销毁前调用；未注册的实例静默忽略）。
        /// </summary>
        public void UnregisterView(ViewPanel view)
        {
            if (view == null) return;
            if (_views.Remove(view))
                UICore.Unregister(view.GetType(), view);
        }

        private void OnDestroy()
        {
            foreach (var view in _views)
            {
                // 传入实例做身份校验，防止场景中存在两个注册器时误注销仍存活的实例
                if (view != null)
                {
                    UICore.Unregister(view.GetType(), view);
                }
            }
            _views.Clear();
        }
    }
}
