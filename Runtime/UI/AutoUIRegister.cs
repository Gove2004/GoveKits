using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 自动将所有子物体上的 ViewPanel 注册到 UICore。
    /// 将此组件挂在场景中任意 GameObject 上即可，无需手动调用 Register。
    /// </summary>
    public class AutoUIRegister : MonoBehaviour
    {
        private ViewPanel[] _panels;

        private void Awake()
        {
            _panels = GetComponentsInChildren<ViewPanel>(true);
            foreach (var panel in _panels)
            {
                UICore.Register(panel.GetType(), panel);
            }
        }

        private void OnDestroy()
        {
            foreach (var panel in _panels)
            {
                if (panel != null)
                {
                    UICore.Unregister(panel.GetType());
                }
            }
            _panels = null;
        }
    }
}
