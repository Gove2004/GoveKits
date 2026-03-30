# GoveKits UI 模块

GoveKits Runtime UI 是游戏界面开发框架，采用 MVVM 架构设计，提供面板管理、数据绑定、自动 UI 收集等功能。开箱即用，与 Core 模块深度整合。

## 目录结构

```
UI/
├── UIController.cs        # UI 控制器 - 面板管理与导航
├── UIElementCollection.cs # UI 元素自动收集基类
├── VMContainer.cs         # ViewModel 单例容器
├── ViewModel.cs           # MVVM 数据模型基类
└── ViewPanel.cs           # UI 面板基类（含泛型版本）
```

## 架构设计

```
┌─────────────────────────────────────────────┐
│                 UIController                │
│  • 面板生命周期管理  • 导航栈  • 面板实例缓存  │
└─────────────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────┐
│                  ViewPanel                  │
│  • 非泛型：基础面板  • 泛型：MVVM 绑定面板     │
└─────────────────────────────────────────────┘
                        │
           ┌────────────┴────────────┐
           ▼                         ▼
┌─────────────────────────┐   ┌───────────────────────┐
│   UIElementCollection   │   │      VMContainer      │
│  • 自动收集 UI 组件      │   │  • ViewModel 单例管理  │
│  • 事件自动路由          │   │  • 懒加载创建/移除     │
└─────────────────────────┘   └───────────────────────┘
                                      │
                                      ▼
                              ┌─────────────────┐
                              │    ViewModel    │
                              │  • 数据变更通知  │
                              │  • SetProperty  │
                              └─────────────────┘
```

## 1. UIController 控制器

UI 系统的核心管理类，负责面板实例化、导航栈管理、面板生命周期控制。

### 使用示例

```csharp
// ===== 1. Inspector 配置 =====
// 在场景中将 UIController 挂载到 GameObject
// 将面板 Prefab 拖入 panelsArray 数组
// 设置 isEntry = true 的面板会自动显示

// ===== 2. 面板导航 =====

// 显示全屏界面
UIController.Instance.Show<MainMenuPanel>();

// 显示弹窗（不进入导航栈）
UIController.Instance.Show<LoadingPopup>();

// 隐藏弹窗
UIController.Instance.HidePopup<LoadingPopup>();

// 返回上一级
UIController.Instance.Back();

// 带参数显示
UIController.Instance.Show<DetailPanel>(new { itemId = 123 });

// ===== 3. 获取面板引用 =====
// 通过 Controller 属性访问
Controller.Show<OtherPanel>();
```

### 注意事项

- 面板必须继承 ViewPanel 并在 Inspector 中配置
- Prefab 和场景节点都支持，系统自动识别
- 弹窗（isPopup = true）不进入导航栈，独立管理
- 导航栈至少保留一个入口界面
- UIController 是 MonoBehaviour，需通过 Instance 或 Controller 引用访问

## 2. VMContainer 容器

ViewModel 单例容器，采用 Spring 风格的依赖注入模式，独立于 UIController 管理。

### 使用示例

```csharp
// ===== 1. 获取 ViewModel 单例 =====
var playerVM = VMContainer.Get<PlayerViewModel>();
playerVM.Name = "玩家 1";

// ===== 2. 在任何地方访问 =====
// 不依赖 UIController，可在任意 C# 代码中使用
var gameVM = VMContainer.Get<GameViewModel>();
gameVM.StartGame();

// ===== 3. ViewModel 跨面板共享 =====
// 面板 A 修改数据
VMContainer.Get<PlayerViewModel>().Level = 10;

// 面板 B 自动收到通知并更新 UI
// （通过 PropertyChanged 事件）

// ===== 4. 移除 ViewModel（可选） =====
// 用于特定场景下清理资源或重置状态
VMContainer.Remove<PlayerViewModel>();
// 下次 Get 时会重新创建新实例
```

### 注意事项

- ViewModel 全局单例，生命周期与应用一致
- 懒加载模式，首次 Get 时创建实例
- 要求 ViewModel 有无参构造函数（new() 约束）
- 与 UIController 解耦，可独立使用
- Remove() 是可选操作，谨慎使用，避免数据丢失
- 线程非安全，请在主线程使用

## 3. ViewModel 数据模型

MVVM 模式的数据层基类，实现属性变更通知，驱动 UI 自动更新。

### 使用示例

```csharp
// ===== 1. 定义 ViewModel =====
public class PlayerViewModel : ViewModel
{
    private string _name;
    private int _level;
    private float _exp;
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public int Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }
    
    public float Exp
    {
        get => _exp;
        set => SetProperty(ref _exp, value);
    }
    
    // 自定义方法
    public void AddExp(float amount)
    {
        Exp += amount;
        // 升级逻辑
        if (Exp >= 100)
        {
            Exp -= 100;
            Level++;
        }
    }
}

// ===== 2. 访问 ViewModel =====
// 通过 VMContainer 获取单例
var playerVM = VMContainer.Get<PlayerViewModel>();

// 修改数据（自动触发通知）
playerVM.Name = "新玩家";
playerVM.AddExp(50);

// 数据在面板间共享
// 多个面板订阅同一 ViewModel，数据同步更新
```

### 注意事项

- 必须使用 SetProperty 才能触发通知
- 属性名通过 CallerMemberName 自动获取
- ViewModel 全局单例，生命周期与应用一致
- 避免在属性 setter 中触发复杂逻辑
- 可重写 OnPropertyChanged 添加自定义逻辑

## 4. ViewPanel 面板基类

UI 面板的基类，提供生命周期管理和 MVVM 数据绑定。

### 使用示例

```csharp
// ===== 1. 非泛型面板（简单界面） =====
public class LoadingPanel : ViewPanel
{
    protected override void OnButtonClicked(string btnName)
    {
        // 处理按钮点击
    }
    
    public override void OnShow(object payload = null)
    {
        base.OnShow(payload);
        TMPTexts["TipText"].text = "加载中...";
    }
}

// ===== 2. 泛型面板（MVVM 绑定） =====
public class PlayerInfoPanel : ViewPanel<PlayerViewModel>
{
    // 数据变更回调 - 必须实现
    protected override void OnDataChanged(object sender, PropertyChangedEventArgs e)
    {
        // 空属性名表示全量刷新（首次显示时）
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            TMPTexts["NameText"].text = ViewModel.Name;
            TMPTexts["LevelText"].text = $"Lv.{ViewModel.Level}";
            Sliders["ExpSlider"].value = ViewModel.Exp / 100f;
            return;
        }
        
        // 按属性名单独更新
        switch (e.PropertyName)
        {
            case nameof(PlayerViewModel.Name):
                TMPTexts["NameText"].text = ViewModel.Name;
                break;
            case nameof(PlayerViewModel.Level):
                TMPTexts["LevelText"].text = $"Lv.{ViewModel.Level}";
                break;
            case nameof(PlayerViewModel.Exp):
                Sliders["ExpSlider"].value = ViewModel.Exp / 100f;
                break;
        }
    }
    
    protected override void OnButtonClicked(string btnName)
    {
        if (btnName == "UpgradeBtn")
        {
            ViewModel.AddExp(50);
        }
    }
}

// ===== 3. 面板配置 =====
// Inspector 中设置：
// - isEntry = true  → 启动时自动显示
// - isPopup = true  → 弹窗模式（不进入导航栈）
```

### 生命周期

```
OnShow(payload)          ← 面板显示时调用
    ├─ 从 VMContainer 获取 ViewModel 单例
    ├─ 订阅 PropertyChanged 事件
    ├─ 触发全量刷新（PropertyName = null）
    └─ 激活 GameObject

OnHide()                 ← 面板隐藏时调用
    ├─ 解绑 PropertyChanged 事件
    ├─ 清空 ViewModel 引用
    └─ 禁用 GameObject
```

### 注意事项

- 泛型面板必须实现 OnDataChanged 抽象方法
- ViewModel 在 OnShow 时绑定，OnHide 时解绑
- 首次显示会触发全量刷新，便于初始化 UI
- 避免在 OnDataChanged 中修改 ViewModel 属性（防止循环触发）
- 弹窗面板（isPopup）不进入导航栈，需手动隐藏

## 5. UIElementCollection 元素收集

自动扫描并绑定 UI 组件，减少手动拖拽，统一事件路由。

### 支持组件

| 组件类型 | 原生 UI          | TextMeshPro      |
|----------|------------------|------------------|
| 按钮     | Button           | -                |
| 开关     | Toggle           | -                |
| 滑块     | Slider           | -                |
| 下拉框   | Dropdown         | TMP_Dropdown     |
| 文本     | Text             | TextMeshProUGUI  |
| 输入框   | InputField       | TMP_InputField   |
| 图片     | Image/RawImage   | -                |

### 使用示例

```csharp
// ===== 1. 继承基类 =====
public class LoginPanel : UIElementCollection
{
    protected override void OnButtonClicked(string btnName)
    {
        switch (btnName)
        {
            case "LoginBtn":
                // 直接通过字典访问组件
                TMPInputFields["UsernameInput"].text = "";
                break;
            case "RegisterBtn":
                UIController.Instance.Show<RegisterPanel>();
                break;
        }
    }
    
    protected override void OnInputChanged(string iName, string val)
    {
        if (iName == "PasswordInput")
        {
            // 密码长度验证
            Buttons["LoginBtn"].interactable = val.Length >= 6;
        }
    }
}

// ===== 2. 访问组件 =====
// 设置文本
TMPTexts["ScoreText"].text = "1000";

// 设置图片
Images["AvatarImage"].sprite = newSprite;

// 设置开关
Toggles["MusicToggle"].isOn = true;

// 获取组件引用
var btn = Buttons["SubmitBtn"];
btn.onClick.AddListener(() => { });
```

### 注意事项

- 组件按 GameObject 名称自动索引，确保名称唯一
- 事件自动绑定，无需手动 AddListener
- 虚方法供子类重写，实现具体交互逻辑
- 支持子对象递归搜索（GetComponentsInChildren）
- 纯显示组件（Text/Image）仅收集，无事件回调

## 完整示例

### 登录流程

```csharp
// 1. 定义 ViewModel
public class LoginViewModel : ViewModel
{
    private string _username;
    private string _password;
    private bool _isLoading;
    
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    
    public async Task<bool> LoginAsync()
    {
        IsLoading = true;
        // 模拟网络请求
        await Task.Delay(1000);
        IsLoading = false;
        return true;
    }
}

// 2. 定义面板
public class LoginPanel : ViewPanel<LoginViewModel>
{
    protected override void OnDataChanged(object sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            // 全量刷新
            TMPInputFields["UsernameInput"].text = ViewModel.Username;
            TMPInputFields["PasswordInput"].text = ViewModel.Password;
            Buttons["LoginBtn"].interactable = !ViewModel.IsLoading;
            return;
        }
        
        switch (e.PropertyName)
        {
            case nameof(LoginViewModel.IsLoading):
                Buttons["LoginBtn"].interactable = !ViewModel.IsLoading;
                TMPTexts["StatusText"].text = ViewModel.IsLoading ? "登录中..." : "";
                break;
        }
    }
    
    protected override async void OnButtonClicked(string btnName)
    {
        if (btnName == "LoginBtn")
        {
            var success = await ViewModel.LoginAsync();
            if (success)
            {
                UIController.Instance.Show<MainMenuPanel>();
            }
        }
    }
}

// 3. 场景配置
// - 挂载 UIController 到场景
// - 将 LoginPanel Prefab 拖入 panelsArray
// - 设置 isEntry = true
```

## 通用注意事项

### 初始化

| 模块                | 是否需要手动初始化        |
|---------------------|---------------------------|
| UIController        | ❌ 自动初始化（Awake）    |
| VMContainer         | ❌ 懒加载（首次 Get 时创建）|
| ViewModel           | ❌ 懒加载（首次 Get 时创建）|
| UIElementCollection | ❌ 自动绑定（Awake）      |
| ViewPanel           | ❌ 自动生命周期           |

### 最佳实践

1. 面板命名：使用 功能 + Panel 格式，如 LoginPanel、SettingsPanel
2. ViewModel 命名：使用 功能 + ViewModel 格式，如 PlayerViewModel
3. UI 组件命名：使用 功能 + 类型 格式，如 SubmitBtn、UsernameInput
4. 事件处理：优先使用 OnDataChanged 响应数据变化，避免手动刷新
5. 内存管理：面板隐藏时自动解绑事件，无需手动处理
6. 职责分离：UIController 管面板，VMContainer 管数据，各司其职
