using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 界面自动注册器。挂在场景中任意 GameObject 上，
    /// 自动扫描并注册所有子物体上的 ViewPanel 到 UICore，销毁时自动注销。
    ///
    /// 用法：场景根物体挂一个本组件，之后即可通过 UICore.Show&lt;T&gt;() 打开任意注册过的界面，无需手动注册。
    /// </summary>
    public class UIAutoRegister : MonoBehaviour
    {
        private ViewPanel[] _views;

        private void Awake()
        {
            _views = GetComponentsInChildren<ViewPanel>(true);
            foreach (var view in _views)
            {
                UICore.Register(view.GetType(), view);
            }
        }

        private void OnDestroy()
        {
            if (_views == null) return;

            foreach (var view in _views)
            {
                // 传入实例做身份校验，防止场景中存在两个注册器时误注销仍存活的实例
                if (view != null)
                {
                    UICore.Unregister(view.GetType(), view);
                }
            }
            _views = null;
        }
    }
}
