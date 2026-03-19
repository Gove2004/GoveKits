# UI/Panel 模块

UI 面板管理系统：基于 Android Activity 生命周期设计的栈式面板导航框架，支持异步过渡动画与弹窗层级管理。

## 核心概念

### 面板栈与导航

采用**栈式（Stack）管理**策略：

- **新面板 Push**：打开新面板时，当前面板 Pause（暂停但保留），新面板 Start→Resume（启动并激活）
- **弹窗特性**：若新面板标记为 `isPopup=true`，则下方面板保持可见；否则 Stop（完全隐藏）
- **返回操作**：Pop 栈顶面板时，触发其 OnStop/OnFinish，恢复下一面板的 Resume 状态

### 面板生命周期

```
         ┌─────────────────┐
         │    OnCreate     │ (首次创建，仅一次)
         └────────┬────────┘
                  ↓
         ┌─────────────────┐
         │    OnStart      │ (激活前，传入参数)
         └────────┬────────┘
                  ↓
         ┌─────────────────┐
         │    OnResume     │ (进入前台，显示动画)
         └─┬────────────┬──┘
           │            │
      继续│            │返回/切换
        ├─↓──────────┤
        │  OnPause   │ (暂停，隐藏上层动画)
        └─┬──────────┘
          ↓
        ┌─────────────┐
        │   OnStop    │ (完全隐藏，保留资源)
        └─┬──────────┘
          ↓
        ┌─────────────┐
        │  OnFinish   │ (销毁，释放资源)
        └─────────────┘
```

**特点**：
- **OnCreate**：仅调用一次（缓存/初始化 UI 组件）
- **OnStart**：每次激活时调用（接收参数、重置状态）
- **OnResume**：进入可交互状态（显示动画）
- **OnPause**：暂停但保留状态（如被新弹窗遮挡）
- **OnStop**：完全隐藏（可释放重资源，下次重新 OnStart）
- **OnFinish**：销毁面板实例

---

## 核心类

### UIController

**职能**：管理一组 UI 面板的生命周期和导航

```csharp
[SerializeField] private BasePanel[] uiPanelsArray;  // 配置所有面板
private Stack<BasePanel> panelStack;                // 栈式管理
```

**关键方法**：

| 方法 | 说明 |
|------|------|
| `Show<T>(payload)` | 异步打开类型为 T 的面板，传入参数 |
| `Hide()` / `Pop()` | 关闭栈顶面板 |
| `GetPanel<T>()` | 获取已注册面板实例 |
| `Clear()` | 清空所有面板栈 |

**使用示例**：

```csharp
// 在 Awake 时自动初始化所有 uiPanelsArray 中的面板
// isEntry=true 的面板自动作为入口面板显示

// 打开面板
uiController.Show<MenuPanel>(payload: new { level = 1 });

// 返回上一面板
uiController.Pop();

// 获取面板实例（已激活或待激活）
var settingPanel = uiController.GetPanel<SettingPanel>();
```

---

### BasePanel

**职能**：UI 面板的抽象基类，提供生命周期模板

```csharp
public bool isEntry = false;    // 是否为首个显示的面板
public bool isPopup = false;    // 是否为弹窗（不隐藏下方面板）
```

**关键属性**：

| 属性 | 说明 |
|------|------|
| `IsCreated` | 是否已调用 OnCreate |
| `CanvasGroup` | 缓存的 CanvasGroup 组件 |
| `uiController` | 持有的 UIController 引用 |

**重写生命周期方法**：

```csharp
public class MyPanel : BasePanel
{
    protected override void OnCreate()
    {
        // 首次创建：获取组件、初始化列表等
        base.OnCreate();
    }

    protected override void OnStart(object payload = null)
    {
        // 激活时：设置 UI 初始状态
        base.OnStart(payload);
    }

    public override void OnResume()
    {
        // 进入可见：播放显示动画
        base.OnResume();
        // 淡入动画（0.3s）
    }

    public override void OnPause()
    {
        // 暂停：不播放隐藏动画（保留下方可见）
        base.OnPause();
    }

    public override void OnStop()
    {
        // 完全隐藏：播放隐藏动画
        base.OnStop();
        // 淡出动画（0.2s）
    }

    public override void OnFinish()
    {
        // 销毁：释放资源
        base.OnFinish();
    }
}
```

---

### IPanelLifeCycle

**职能**：面板生命周期接口（内部接口，通过 UIController 驱动）

```csharp
internal interface IPanelLifeCycle
{
    void OnCreate();
    void OnStart(object payload = null);
    void OnResume();
    void OnPause();
    void OnStop();
    void OnFinish();
}
```

**优点**：通过显式接口实现，生命周期方法无法被外部直接调用，只能由 UIController 驱动，保证状态管理的唯一性与安全性。

---

### PanelEvent

**职能**：面板生命周期事件，用于订阅面板状态变化

```csharp
public class PanelEvent : EventInfo
{
    public PanelLifeType LifeType { get; set; }
    public BasePanel Panel { get; set; }
}

public enum PanelLifeType
{
    OnCreate, OnStart, OnResume, OnPause, OnStop, OnFinish
}
```

**订阅示例**：

```csharp
// 监听所有面板事件
EventManager.Subscribe<PanelEvent>((evt) =>
{
    Debug.Log($"面板 {evt.Panel.name} 触发事件：{evt.LifeType}");
});
```

---

### UIItem

**职能**：UI 项目基类，表示面板中的单个可交互组件

```csharp
public abstract class UIItem : MonoBehaviour
{
    // 子类实现具体交互逻辑
}
```

---

## 完整使用示例

### 1. 定义面板

```csharp
public class MainMenuPanel : BasePanel
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingButton;

    protected override void OnCreate()
    {
        base.OnCreate();
        startButton.onClick.AddListener(() =>
        {
            uiController.Show<GamePanel>();
        });
        settingButton.onClick.AddListener(() =>
        {
            uiController.Show<SettingPanel>(isPopup: true);
        });
    }

    protected override void OnStart(object payload = null)
    {
        base.OnStart(payload);
        // 重置主菜单状态
    }

    public override void OnResume()
    {
        base.OnResume();
        // 淡入动画
    }

    public override void OnStop()
    {
        base.OnStop();
        // 淡出动画
    }
}

public class SettingPanel : BasePanel
{
    [SerializeField] private Button closeButton;

    protected override void OnCreate()
    {
        base.OnCreate();
        closeButton.onClick.AddListener(() =>
        {
            uiController.Pop();  // 返回
        });
    }

    // isPopup=true，所以只播放淡入/淡出，不触发下方面板的 Stop/Resume
}
```

### 2. 配置 UIController

```
Canvas
  ├─ UIController (脚本)
  │   ├─ Main Menu Panel
  │   ├─ Game Panel
  │   ├─ Setting Panel (isPopup=true)
  │   └─ Loading Panel
```

在 Inspector 中将这些面板拖入 UIController 的 `uiPanelsArray` 数组，并标记 MainMenuPanel 的 `isEntry=true`。

### 3. 启动应用

```csharp
// UIController 会在 Awake 时：
// 1. 初始化所有面板（SetActive=false）
// 2. 自动显示 isEntry=true 的面板
// 3. 之后通过 Show/Pop 进行导航
```

---

## 最佳实践

1. **面板粒度**：每个独立功能模块（主菜单、游戏、设置等）创建一个面板
2. **数据传递**：通过 `Show<T>(payload)` 传递参数，在 `OnStart(payload)` 中使用
3. **弹窗特性**：设置 `isPopup=true` 的面板不会隐藏下方面板（适合设置、确认框等）
4. **动画时长**：OnResume 显示动画（0.3s）、OnStop 隐藏动画（0.2s）、OnPause 无动画
5. **监听事件**：通过 `PanelEvent` 在全局监听面板状态，便于埋点、日志记录

---

## 相关文档

- [Events 事件系统](../../../Utility/Events/README.md)
- [UI/MVI 架构参考](../MVI/README.md)
