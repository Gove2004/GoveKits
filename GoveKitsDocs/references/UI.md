# Runtime/UI —— UI 框架

MVVM 风格的 UI 框架：一个界面（`ViewPanel`）绑定一个数据模型（`ViewModel`），界面内的 UI 组件与可复用小组件（`UIItem`）由 `UIElements` 自动收集，`UICore` 统一注册与开关界面。

## 模块架构

```
Runtime/UI/
├── UICore.cs             门面 —— 界面注册/显示 + ViewModel 持有
├── Collection/           组件收集
│   └── UIElements.cs     自动收集 UI 组件与 UIItem，按名称索引
├── Hierarchy/            层级管理
│   ├── ViewPanel.cs      界面面板（绑定 ViewModel）
│   └── UIItem.cs         可复用小组件
├── Lifecycle/            生命周期
│   └── UIAutoRegister.cs 场景自动注册界面
└── MVVM/                 数据驱动
    └── ViewModel.cs      界面数据与业务逻辑
```

## 场景搭建

```
场景根: UIAutoRegister（自动注册所有 ViewPanel）

LoginPanel (挂 LoginPanel 脚本，自动补 UIElements，勾选 enableButtons / enableTMPTexts)
├── LoginBtn            (Button)
├── StatusText          (TMP Text)
└── UserNameInput       (挂 InputItem 脚本，自动补 UIElements)
    └── InputField      (TMP_InputField)
```

## UIElements —— 组件收集

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
// 注意：Rebind 会对收集到的组件调用 RemoveAllListeners——外部直接 onClick.AddListener 注册的
// 监听会被清除；交互请统一走 ButtonClicked 等公共事件（其订阅在 Rebind 后保留）。
```

支持的组件：Button / Toggle / Slider / Dropdown / Image / RawImage / Text / InputField / TMP 文本 / TMP 输入框 / TMP 下拉框，各有一个 Inspector 开关。同名组件自动报警告并忽略。

## ViewPanel —— 界面

一个界面 = 一个面板，绑定一个 ViewModel。继承 `ViewPanel<TVM>` 自动完成 VM 绑定与生命周期。

```csharp
public class LoginPanel : ViewPanel<LoginVM>
{
    protected override void OnEnable()
    {
        base.OnEnable();                                  // 必须调用（自动绑定 VM + 全量刷新）
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

> 面板已激活时重复 Show 仅刷新参数（OnReceiveShowParam），不重复触发 OnShow。

| 重写方法 | 时机 | 用途 |
|---|---|---|
| `OnNotify(key)` | VM 数据变化 | 按 key 刷新控件；key=null 全量刷新 |
| `OnReceiveShowParam(param)` | Show 时 | 接收 UICore.Show 传入的参数 |
| `OnShow()` | 激活完成后 | 入场动画、初始化展示 |
| `OnHide()` | 失活前 | 退场动画 |

## UIItem —— 可复用小组件

可复用的 UI 部件：内部组件由自身 UIElements 收集（自包含），父级界面的 UIElements 自动收集并注册它（按名称索引）。适合输入框、数量选择器、金币条、道具格子等。

```csharp
// 1. 定义小组件 —— 内部组件勾选 enableTMPInputFields 即可使用
public class InputItem : UIItem
{
    public string Value => Elements.TMPInputFields["InputField"].text;
    public void SetValue(string v) => Elements.TMPInputFields["InputField"].text = v;
    public void Clear() => Elements.TMPInputFields["InputField"].text = "";
}

// 2. 定义带交互的小组件 —— 内部按钮勾选 enableButtons，订阅自身事件
public class QuantityItem : UIItem
{
    [SerializeField] private int _max = 99;

    public int Value { get; private set; } = 1;

    protected override void Awake()
    {
        base.Awake();                                     // 必须调用
        Elements.ButtonClicked += OnClick;                // 订阅内部按钮
    }

    protected override void OnDestroy()
    {
        Elements.ButtonClicked -= OnClick;                // 防泄漏
    }

    private void OnClick(string btn)
    {
        if (btn == "AddBtn") Value = Mathf.Min(Value + 1, _max);
        if (btn == "SubBtn") Value = Mathf.Max(Value - 1, 1);
        OnValueChanged?.Invoke(Value);                    // 对外事件
    }

    public event Action<int> OnValueChanged;              // 面板可订阅
}

// 3. 面板内访问（父级 UIElements 自动收集）
Elements.GetItem<InputItem>("UserNameInput").Value;
Elements.GetItem<QuantityItem>("CountItem").OnValueChanged += n => { ... };
```

- `UIItem` 挂到界面内子物体上，RequireComponent 自动补 UIElements，勾选开关即可用 `Elements` 访问内部组件
- 父级界面的收集器**自动跳过 Item 子树**（内部由 Item 自身的收集器管），互不干扰
- 支持**嵌套**：外层收集器扁平收集子树内全部下层 Item，外层可直接 `Elements.Items["内层Item名"]` 访问
- Item 被父级按名称注册：`Elements.Items["UserNameInput"]`；同名只收录先到的一个，后到的输出警告并忽略

## UIAutoRegister —— 场景自动注册

场景根挂 `UIAutoRegister` 后，Awake 自动扫描注册全部子物体上的 ViewPanel（含未激活），销毁时自动注销。

运行时动态 `Instantiate` 出来的界面**不会经过 Awake 扫描**，需手动注册/注销：

```csharp
GetComponent<UIAutoRegister>().RegisterView(newPanel);     // 注册动态界面（重复注册忽略）
GetComponent<UIAutoRegister>().UnregisterView(newPanel);   // 注销（界面销毁前调用，未注册则静默）
```

## ViewModel —— 数据驱动

界面数据与业务逻辑，与 ViewPanel 一一对应（由 UICore 按类型持有）。

```csharp
public class LoginVM : ViewModel
{
    public string UserName { get; private set; }
    public string Password { get; private set; }
    public string Status { get; private set; }
    public bool IsBusy { get; private set; }

    // 首次创建时自动调用一次（读存档、预加载等）
    public override void OnInit() { Status = "待登录"; }

    // 数据变化 → Notify(key) → 界面 OnNotify(key) 刷新
    public void SetInput(string user, string pwd)
    {
        UserName = user; Password = pwd;
        Notify("input");                                 // 局部刷新
    }

    public async void TryLogin()
    {
        IsBusy = true; Status = "登录中...";
        Notify("status");                                // 通知界面刷新状态区

        await UniTask.Delay(500);                        // 模拟请求

        IsBusy = false;
        Status = UserName == "admin" ? "登录成功" : "用户名或密码错误";
        Notify("status");
    }
}
```

方法一览：

| 方法 | 谁调用 | 作用 |
|---|---|---|
| `OnInit()` | UICore.GetVM 首次创建 | 初始化数据（重写） |
| `Notify(key)` | VM 内部（protected） | 通知界面刷新；key=null 全量刷新 |
| `AttachView(view)` | ViewPanel 自动 | 绑定界面 + 推一次全量刷新 |
| `DetachView(view)` | ViewPanel 自动 | 解绑界面 |

- 界面内：`VM` 属性直接访问；界面外：`UICore.GetVM<LoginVM>()` 拿到**同一实例**
- 多个界面切换时 VM 各自独立持有，互不污染

## UICore —— 门面

```csharp
UICore.Show<LoginPanel>();                // 打开界面
UICore.Show<LoginPanel>(userData);        // 带参数打开（OnReceiveShowParam 接收）
UICore.Hide<LoginPanel>();                // 关闭界面
UICore.GetVM<LoginVM>().Status;           // 界面外读写数据（同一实例）
```

## 多界面切换

界面之间切换 = 一个 Show + 一个 Hide，生命周期自动衔接（旧界面 OnHide → 失活解绑，新界面 OnReceiveShowParam → 激活绑定 → OnShow）。

**原则：导航（切换界面）属于业务逻辑，在 ViewModel 的方法里执行；View 只调 VM 方法，不直接操作 UICore。**

```csharp
// View 侧：只调 VM 方法
public class LoginPanel : ViewPanel<LoginVM>
{
    private void OnClick(string btn)
    {
        if (btn == "LoginBtn")
            VM.OnLoginClick(Elements.GetItem<InputItem>("UserNameInput").Value);
    }
}

// VM 侧：业务逻辑 + 导航都在这里
public class LoginVM : ViewModel
{
    public string UserName { get; private set; }
    public string Status { get; private set; }

    public void OnLoginClick(string user)
    {
        UserName = user;
        Status = "登录中...";
        Notify("status");

        // ... 登录校验 / 请求逻辑 ...
        bool success = user == "admin";

        Status = success ? "登录成功" : "用户名或密码错误";
        Notify("status");

        if (!success) return;

        UICore.Hide<LoginPanel>();                 // 导航：关登录
        UICore.Show<MainMenuPanel>(UserName);      // 导航：开主菜单并传参
    }
}

// 主菜单接收参数
public class MainMenuPanel : ViewPanel<MainMenuVM>
{
    public override void OnReceiveShowParam(object param)
    {
        VM.PlayerName = (string)param;
    }
    // OnNotify(null) 绑定即全量刷新 → 显示玩家名
}
```

其他常见模式（同样写在 VM 方法里）：

```csharp
// 返回上一界面
public void OnBackClick() { UICore.Hide<MainMenuPanel>(); UICore.Show<LoginPanel>(); }

// 已激活面板重复 Show 只刷新参数，不会重复触发 OnShow，无需手动先 Hide
public void OnOpenSettings() { UICore.Show<SettingsPanel>(); }
```

## 完整示例（登录 → 主菜单）

```csharp
// 1. VM —— 数据 + 业务 + 导航
public class LoginVM : ViewModel
{
    public string UserName { get; private set; }
    public string Status { get; private set; }

    public void OnLoginClick(string user)
    {
        UserName = user;
        Status = "登录中...";
        Notify("status");

        // ... 登录校验 / 请求逻辑 ...
        bool success = user == "admin";

        Status = success ? "登录成功" : "用户名或密码错误";
        Notify("status");

        if (success)
        {
            UICore.Hide<LoginPanel>();                 // 导航在 VM 里执行
            UICore.Show<MainMenuPanel>(UserName);
        }
    }
}

// 2. Panel —— 登录界面（只调 VM 方法 + 渲染）
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
        if (key == null || key == "status")
            Elements.TMPTexts["StatusText"].text = VM.Status;
    }
    private void OnClick(string btn)
    {
        if (btn == "LoginBtn")
            VM.OnLoginClick(Elements.GetItem<InputItem>("UserNameInput").Value);
    }
}

// 3. Panel —— 主菜单界面
public class MainMenuPanel : ViewPanel<MainMenuVM>
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
        if (key == null)                                  // 绑定即全量刷新
            Elements.TMPTexts["WelcomeText"].text = $"欢迎，{VM.PlayerName}";
    }
    public override void OnReceiveShowParam(object param)
    {
        VM.PlayerName = (string)param;
    }
    private void OnClick(string btn)
    {
        if (btn == "LogoutBtn") VM.OnLogoutClick();       // 返回登录也走 VM
    }
}

// 4. 启动
UICore.Show<LoginPanel>();
```

## 数据流

```
外部逻辑 UICore.GetVM<LoginVM>()  ←── 同一实例 ──→ 界面内 VM 属性
     │                                      │
     └── VM.Login() → Notify("status") → ViewPanel.OnNotify("status") → 刷新控件
```
