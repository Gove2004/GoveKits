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
            foreach (var view in _views)
            {
                if (view != null)
                {
                    UICore.Unregister(view.GetType());
                }
            }
            _views = null;
        }
    }
}
