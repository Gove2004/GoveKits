using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 可复用小组件基类（Item 层）。
    /// 一个小组件 = 可复用的 UI 部件（如用户名输入框、数量选择器、金币条），内部组件由自身的 UIElements 收集。
    ///
    /// 用法：
    /// 1. 继承本类实现小组件逻辑（如 InputItem 提供 Value / Clear 接口）
    /// 2. 挂到界面内的子物体上（RequireComponent 自动添加 UIElements，勾选收集开关即可访问内部组件）
    /// 3. 父级界面的 UIElements 会自动收集本组件，外部通过 Elements.GetItem&lt;T&gt;("名字") 访问
    ///
    /// 注意：Item 内部组件由 Item 自身的 UIElements 收集，父级界面的收集器会自动跳过 Item 子树，互不干扰。
    /// 支持嵌套：外层收集器会扁平收集其子树内全部下层 Item（外层可通过 Items["名字"] 直接访问内层 Item）；
    /// 同名 Item 只收录先到的一个，后到的输出警告并忽略。
    /// </summary>
    [RequireComponent(typeof(UIElements))]
    public abstract class UIItem : MonoBehaviour
    {
        /// <summary>组件收集器（收集本小组件内部的 UI 组件）。Inspector 自动填充，无需手动赋值。</summary>
        [SerializeField] protected UIElements Elements;

        /// <summary>确保 Elements 引用有效（Inspector 未赋值时自动获取）。</summary>
        protected virtual void Awake()
        {
            if (Elements == null) Elements = GetComponent<UIElements>();
        }
    }
}
