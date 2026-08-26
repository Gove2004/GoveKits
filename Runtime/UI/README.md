# UI 模块

UI 模块提供 MVVM 风格的面板管理与 ViewModel 基础设施。通过 `ViewPanel` + `ViewModel` 的双向绑定模式，将 UI 展示逻辑与业务数据分离，配合 `AutoUIRegister` 和 `UIElementCollection` 实现自动化组件管理。

## 架构概览

```
UICore（UI 门面，实现 ICore）
├── 面板注册 / Show / Hide
└── ViewModel 单例工厂（GetVM<T>）

AutoUIRegister（MonoBehaviour）
└─ Awake 扫描所有 ViewPanel → UICore.Register

UIElementCollection（MonoBehaviour）
├─ enableXXX 开关控制组件收集
└─ 单次遍历 UIBehaviour 分发到字典

ViewPanel（抽象基类）
└─ OnNotify / OnBindVM / OnUnbindVM / OnShow / OnHide

ViewPanel<TVM>（泛型）
└─ 自动绑定 ViewModel：VM = UICore.GetVM<TVM>()

ViewModel（抽象基类）
├─ AttachView / DetachView
└─ NotifyViews(string key) — 倒序遍历通知所有绑定视图
```

## 用法

### 1. 定义 ViewModel

ViewModel 管理业务数据和状态，通过 `NotifyViews(key)` 通知视图更新：

```csharp
public class GameVM : ViewModel
{
    public int Score { get; set; }

    public override void OnInit()
    {
        Score = 0;
    }

    public void AddScore(int value)
    {
        Score += value;
        NotifyViews(nameof(Score));  // 通知所有绑定此 ViewModel 的视图
    }

    public void ResetScore()
    {
        Score = 0;
        NotifyViews(nameof(Score));
    }
}
```

### 2. 定义面板

继承 `ViewPanel<TVM>` 自动获得 ViewModel 绑定：

```csharp
public class GamePanel : ViewPanel<GameVM>
{
    public Text ScoreText;  // 通过 Inspector 赋值或 GetComponent 获取

    public override void OnNotify(string key)
    {
        switch (key)
        {
            case nameof(GameVM.Score):
                ScoreText.text = VM.Score.ToString();
                break;
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        ScoreText.text = VM.Score.ToString();  // 面板显示时刷新
    }

    public override void OnHide()
    {
        base.OnHide();
    }
}
```

### 3. 显示 / 隐藏面板

```csharp
// 显示面板（首次调用时自动创建实例）
UICore.Show<GamePanel>();

// 隐藏面板
UICore.Hide<GamePanel>();
```

### 4. 自动注册

在场景中放置 `AutoUIRegister` 组件，它会在 `Awake` 中自动发现所有子物体上的 `ViewPanel` 并注册到 `UICore`：

```
Canvas
└── AutoUIRegister（挂载此组件）
    ├── GamePanel（子物体上的 ViewPanel 实例）
    └── ShopPanel
```

## UIElementCollection 组件开关

继承 `UIElementCollection` 后，在编辑器中勾选需要的组件类型即可自动收集并绑定事件：

| 开关字段 | 对应组件 |
|---------|---------|
| `enableButtons` | `Button` |
| `enableToggles` | `Toggle` |
| `enableSliders` | `Slider` |
| `enableDropdowns` | `Dropdown` |
| `enableTMPDropdowns` | `TMP_Dropdown` |
| `enableInputFields` | `InputField` |
| `enableTMPInputFields` | `TMP_InputField` |
| `enableTexts` | `Text` |
| `enableTMPTexts` | `TextMeshProUGUI` |
| `enableImages` | `Image` |
| `enableRawImages` | `RawImage` |

未勾选的类型不会扫描子物体，也不会创建字典和绑定事件，节省性能。

## ViewModel 生命周期

```
创建 → OnInit() → AttachView(view) → 业务调用 NotifyViews() → DetachView(view) → 销毁
```

| 方法 | 调用时机 | 说明 |
|------|---------|------|
| `OnInit()` | 首次 `GetVM<T>()` 时 | 初始化数据 |
| `AttachView()` | 面板 `OnShow` 时 | 绑定视图引用 |
| `DetachView()` | 面板 `OnHide` 时 | 解除视图引用 |
| `NotifyViews(key)` | 数据变更时 | 通知所有绑定的视图更新 |

## ViewPanel 生命周期

```
创建 → OnBindVM() → OnShow() → OnNotify(key) → OnHide() → OnUnbindVM() → 销毁
```

| 方法 | 调用时机 | 说明 |
|------|---------|------|
| `OnBindVM()` | 面板显示且 VM 绑定后 | 获取 UI 组件引用 |
| `OnUnbindVM()` | 面板隐藏且 VM 解绑后 | 清理引用 |
| `OnShow()` | 面板变为可见时 | 刷新显示 |
| `OnHide()` | 面板变为不可见时 | 清理状态 |
| `OnNotify(key)` | ViewModel 数据变更时 | 响应特定字段的更新 |

## 注意事项

- **面板为单例模式**：每种类型只有一个实例，暂不支持同类型多实例同时显示
- **面板必须放置在场景中**：通过 `AutoUIRegister` 或手动 `UICore.Register(panel)` 注册
- **ViewModel 通过 AttachView / DetachView 管理视图引用**：倒序遍历防止集合修改异常
- **NotifyViews 的 key 应与 ViewModel 的 public property 名称一致**：使用 `nameof()` 确保编译期安全
- **UICore 由 GoveCore 统一管理生命周期**：`GoveCore.Close()` 会自动关闭所有面板和 ViewModel
