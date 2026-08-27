# Runtime/UI —— UI 框架

MVVM 风格的 UI 框架：组件收集、生命周期、数据驱动、层级管理四大职责。
一个界面（`ViewPanel`）绑定一个数据模型（`ViewModel`），界面内的 UI 组件与可复用小组件（`UIItem`）由 `UIElements` 自动收集。

## 目录与职责

| 目录 | 职责 | 类 |
|---|---|---|
| `Collection/` | 组件收集 | `UIElements` —— 自动收集 UI 组件与 UIItem |
| `Hierarchy/` | 层级管理 | `ViewPanel`（界面）、`UIItem`（小组件） |
| `Lifecycle/` | 生命周期 | `UIAutoRegister` —— 场景自动注册界面 |
| `MVVM/` | 数据驱动 | `ViewModel` —— 界面数据与业务逻辑 |
| （根） | 门面 | `UICore` —— 界面开关 + VM 访问 |

## 场景搭建

```
LoginPanel (挂 LoginPanel 脚本，自动补 UIElements，勾选 enableButtons / enableTMPTexts)
├── LoginBtn              (Button)
├── StatusText            (TMP Text)
└── UserNameInput         (挂 InputItem 脚本，自动补 UIElements)
    └── InputField        (TMP_InputField)
场景根: UIAutoRegister
```

## 一、UIElements —— 组件收集

挂在界面（ViewPanel 自动添加）或小组件（UIItem 自动添加）上，收集子物体的 UI 组件与 UIItem，按名称索引。

```csharp
// Inspector 勾选 enableButtons 后：
Elements.Buttons["LoginBtn"].interactable = false;          // 组件访问
Elements.TMPTexts["StatusText"].text = "登录中...";

// 交互事件（参数均为组件名称）：
Elements.ButtonClicked += name => { if (name == "LoginBtn") OnLogin(); };
Elements.ToggleChanged  += (name, val) => { ... };
Elements.SliderChanged  += (name, val) => { ... };
Elements.InputChanged   += (name, val) => { ... };

// 小组件访问：
Elements.GetItem<InputItem>("UserNameInput").Value;          // 或 Elements.Items["..."]
Elements.GetItem("GoldBar");                                 // 按名称取 UIItem 基类

// 运行时动态增删 UI 后重新收集：
Elements.Rebind();
```

- 支持的组件：Button / Toggle / Slider / Dropdown / Image / RawImage / Text / InputField / TMP 文本 / TMP 输入框 / TMP 下拉框，各有一个 Inspector 开关
- 同名组件自动报警告并忽略（避免静默覆盖）

## 二、ViewPanel —— 界面

一个界面 = 一个面板，绑定一个 ViewModel。继承 `ViewPanel<TVM>` 自动完成 VM 绑定。

```csharp
public class LoginPanel : ViewPanel<LoginVM>
{
    protected override void OnEnable()
    {
        base.OnEnable();                                  // 必须调用（自动绑定 VM）
        Elements.ButtonClicked += OnClick;                // 订阅交互
    }

    protected override void OnDisable()
    {
        base.OnDisable();                                 // 必须调用（自动解绑 VM）
        Elements.ButtonClicked -= OnClick;                // 解绑事件，防泄漏
    }

    public override void OnNotify(string key)
    {
        if (key == null)                                  // 全量刷新（绑定 VM 时自动触发）
        {
            Elements.TMPTexts["StatusText"].text = VM.Status;
            return;
        }
        if (key == "status")
            Elements.TMPTexts["StatusText"].text = VM.Status;
    }

    private void OnClick(string btnName)
    {
        if (btnName == "LoginBtn") VM.TryLogin();
    }
}
```

生命周期（由 UICore 驱动，无需手动调用）：

```
Show: OnReceiveShowParam(param) → 激活(自动绑 VM + 全量刷新) → OnShow()
Hide: OnHide() → 失活(自动解绑 VM)
```

| 重写方法 | 时机 | 用途 |
|---|---|---|
| `OnNotify(key)` | VM 数据变化 | 按 key 刷新控件；key=null 全量刷新 |
| `OnReceiveShowParam(param)` | Show 时 | 接收 UICore.Show 传入的参数 |
| `OnShow()` | 激活完成后 | 入场动画、初始化展示 |
| `OnHide()` | 失活前 | 退场动画 |

## 三、UIItem —— 可复用小组件

可复用的 UI 部件，内部组件由自身 UIElements 收集，被父级界面自动收集注册。

```csharp
public class InputItem : UIItem
{
    // 勾选 enableTMPInputFields 后：
    public string Value => Elements.TMPInputFields["InputField"].text;
    public void Clear() => Elements.TMPInputFields["InputField"].text = "";
}
// 面板内：Elements.GetItem<InputItem>("UserNameInput").Value
```

## 四、ViewModel —— 数据驱动

界面数据与业务逻辑，与 ViewPanel 一一对应。数据变化 → `Notify(key)` → 界面 `OnNotify(key)` 刷新。

```csharp
public class LoginVM : ViewModel
{
    public string Status { get; private set; }

    public override void OnInit() { /* 首次创建时初始化（读存档等） */ }

    public void TryLogin()
    {
        Status = "登录中..."; Notify("status");
        // ... 登录逻辑
        Status = "登录成功";   Notify("status");
    }
}
```

- VM 实例由 `UICore` 按类型持有：界面内 `VM` 属性、界面外 `UICore.GetVM<LoginVM>()` 拿到**同一实例**
- `Notify(null)` = 全量刷新；界面绑定 VM 时自动触发一次全量刷新

## 五、UICore —— 门面

```csharp
UICore.Show<LoginPanel>();                // 打开界面（可传参：Show<LoginPanel>(userData)）
UICore.Hide<LoginPanel>();                // 关闭界面
UICore.GetVM<LoginVM>().Status;           // 界面外读写数据（同一实例）
```

## 完整示例（登录界面）

```csharp
// 1. VM —— 数据与逻辑
public class LoginVM : ViewModel
{
    public string UserName { get; private set; }
    public string Status { get; private set; }
    public void Login(string user) { UserName = user; Status = "登录中..."; Notify("status"); }
}

// 2. Panel —— 界面
public class LoginPanel : ViewPanel<LoginVM>
{
    protected override void OnEnable()
    {
        base.OnEnable();
        Elements.ButtonClicked += OnClick;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        Elements.ButtonClicked -= OnClick;
    }
    public override void OnNotify(string key)
    {
        if (key == null)
            Elements.TMPTexts["StatusText"].text = VM.Status;
    }
    private void OnClick(string btn)
    {
        if (btn == "LoginBtn")
            VM.Login(Elements.GetItem<InputItem>("UserNameInput").Value);
    }
}

// 3. 打开界面
UICore.Show<LoginPanel>();
```

## 数据流

```
外部逻辑 UICore.GetVM<LoginVM>()  ←── 同一实例 ──→ 界面内 VM 属性
     │                                      │
     └── VM.Login() → Notify("status") → ViewPanel.OnNotify("status") → 刷新控件
```
