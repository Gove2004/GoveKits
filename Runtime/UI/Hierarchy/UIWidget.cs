using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 小组件基类（Widget 层）。
    /// 一个小组件 = 可复用的 UI 部件（如用户名输入框、密码输入框、数量选择器、金币条），
    /// 内部结构自包含，被 UIPanel 自动检测并注册（按名称索引），对外暴露方法/事件供面板调用。
    ///
    /// 内部组件管理两种方式：
    /// 1. 序列化字段直接引用（组件少时推荐）
    /// 2. 自身挂 UIElements 收集（组件多时），此时面板的 UIElements 会自动跳过本 Widget 内部组件
    /// </summary>
    public abstract class UIWidget : MonoBehaviour
    {
        // 子类实现具体组件逻辑，对外暴露方法/事件供面板调用
    }
}
