using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 自动将所有子物体上的 ViewPanel（界面）注册到 UICore。
    /// 将此组件挂在场景中任意 GameObject 上即可，无需手动调用 Register。
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
