using UnityEngine;


namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 自动将所有子物体上的 ViewPanel 注册到 UICore
    /// </summary>
    public class AutoUIRegister : MonoBehaviour
    {
        private void Awake()
        {
            var panels = GetComponentsInChildren<ViewPanel>(true);
            foreach (var panel in panels)
            {
                UICore.Register(panel.GetType(), panel);
            }
        }


        private void OnDestroy()
        {
            var panels = GetComponentsInChildren<ViewPanel>(true);
            foreach (var panel in panels)
            {
                UICore.UnRegister(panel.GetType());
            }
        }
    }
}