using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 小组件基类（Item 层）。
    /// 一个小组件 = 可复用的 UI 部件（如用户名输入框、数量选择器、金币条），
    /// 内部组件由自身挂载的 UIElements 收集，被 ViewPanel 自动检测并注册（按名称索引），
    /// 对外暴露方法/事件供面板调用。
    ///
    /// 层级：ViewPanel（界面）→ UIItem（小组件）
    ///
    /// 组件访问：Elements.Buttons["BtnName"]、Elements.TMPTexts["Text"] 等
    /// </summary>
    [RequireComponent(typeof(UIElements))]
    public abstract class UIItem : MonoBehaviour
    {
        /// <summary>组件收集器（收集本小组件内部的 UI 组件），Inspector 自动填充。</summary>
        [SerializeField] protected UIElements Elements;

        protected virtual void Awake()
        {
            if (Elements == null) Elements = GetComponent<UIElements>();
        }
    }
}
