# UI/MVI 模块

MVI（Model-View-Intent）架构参考实现：用于构建响应式、可测试的复杂 UI 系统。

**注**：大部分实现文件（Model.cs、View.cs、System.cs、App.cs）当前为注释状态，作为架构模板供参考。核心骨架已准备就绪，可根据具体需求激活并定制。

## 核心概念

### MVI 架构原理

MVI 是 Elm 架构在 UI 层的应用，分离关注点（单向数据流）：

```
┌─────────────┐          ┌──────────────┐          ┌─────────────┐
│   View      │          │   Intent     │          │   Model     │
│ (UI/渲染)   │────────→ (用户操作) ────→          │ (状态/数据) │
│             │          │   (事件)     │          │             │
└─────────────┘          └──────────────┘          └──────┬──────┘
      ↑                                                    │
      │                                                    │
      └────────────────────────────────────────────────────┘
                (状态变化通知、自动UI更新)
```

**单向数据流**：
1. **View**（视图）显示状态并接收用户输入
2. **Intent**（意图）捕获用户操作或事件
3. **Model**（模型）接收意图，更新内部状态
4. **System**（系统）协调 Model/View，处理业务逻辑
5. **View** 订阅状态变化，自动重绘 UI

---

## 核心类

### State

**职能**：表示 UI 系统的完整状态快照

```csharp
public class IState
{
    // 子类示例
}

// 使用示例
public class GameState : IState
{
    public int Score { get; set; }
    public int HP { get; set; }
    public bool IsGameOver { get; set; }
}
```

**特性**：
- 不可变（Immutable）：每次状态变化创建新对象而非修改原对象
- 完整性：包含 UI 所需的所有数据
- 可比较性：支持深度比较以检测实际变化

---

### Intent

**职能**：表示用户操作或系统事件的意图

```csharp
public interface IIntent
{
    // 子类示例
}

// 使用示例
public class MoveIntent : IIntent
{
    public Direction Direction { get; set; }
}

public class AttackIntent : IIntent
{
    public Vector3 TargetPos { get; set; }
}
```

---

### Module & IModule

**职能**：MVI 系统中所有组件的基类，提供生命周期管理

```csharp
public interface IModule
{
    void Initialize();  // 初始化
    void Dispose();     // 清理资源
}

public abstract class Module : IModule
{
    // 通用模块功能
}
```

**特性**：
- 生命周期管理（Initialize/Dispose）
- 依赖注入支持（可通过 App 容器注册）
- 模块化架构（易于测试、扩展）

---

## 架构组件（注释状态参考）

### Model（模型）

管理状态与响应式更新：

```csharp
// 伪代码（详见 Model.cs 注释）
public abstract class Model<TState> : Module where TState : IState, new()
{
    protected TState currentState = new TState();
    
    public TState CurrentState => currentState;
    
    public void UpdateState(Action<TState> updater)
    {
        updater?.Invoke(currentState);
        NotifyStateChanged();
    }
    
    public Action AddStateListener(Action<TState> listener)
    {
        // 返回取消订阅函数
    }
}
```

---

### View（视图）

响应状态变化并接收用户输入：

```csharp
// 伪代码（详见 View.cs 注释）
public abstract class View<TState> : Module where TState : IState
{
    protected Model<TState> boundModel;
    
    public void BindModel(Model<TState> model)
    {
        // 订阅模型的状态变化
    }
    
    protected abstract void OnStateChanged(TState state);
    
    protected void SendIntent(IIntent intent)
    {
        // 发送意图到系统处理
    }
}
```

---

### System（系统）

协调 Model 和 View，处理业务逻辑：

```csharp
// 伪代码（详见 System.cs 注释）
public abstract class System : Module
{
    protected Dictionary<Type, Model<IState>> models;
    protected Dictionary<Type, View<IState>> views;
    
    public abstract void ProcessIntent(IIntent intent);
    
    public TModel GetModel<TModel>() where TModel : Model<IState>
    {
        // 获取模型实例
    }
    
    public TView GetView<TView>() where TView : View<IState>
    {
        // 获取视图实例
    }
}
```

---

### App（应用）

全局单例，管理所有 MVI 系统：

```csharp
// 伪代码（详见 App.cs 注释）
public class App : MonoBehaviour
{
    private static App instance;
    private Dictionary<string, System> systems;
    
    public void RegisterSystem(System system)
    {
        systems[system.ModuleId] = system;
    }
    
    public TSystem GetSystem<TSystem>() where TSystem : System
    {
        // 获取系统
    }
    
    public void ProcessIntent(IIntent intent)
    {
        // 广播意图到所有系统
    }
}
```

---

## 使用示例（架构模板）

### 1. 定义状态

```csharp
public class PlayerState : IState
{
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Gold { get; set; }
    
    public PlayerState Clone()
    {
        return new PlayerState
        {
            Level = this.Level,
            Experience = this.Experience,
            Gold = this.Gold
        };
    }
}
```

### 2. 定义意图

```csharp
public class GainExperienceIntent : IIntent
{
    public int Amount { get; set; }
}

public class LevelUpIntent : IIntent
{
}
```

### 3. 实现模型

```csharp
public class PlayerModel : Model<PlayerState>
{
    public PlayerModel()
    {
        currentState = new PlayerState { Level = 1, Experience = 0, Gold = 0 };
    }
    
    public void GainExperience(int amount)
    {
        UpdateState(state =>
        {
            state.Experience += amount;
            if (state.Experience >= 100)
            {
                state.Level++;
                state.Experience = 0;
            }
        });
    }
}
```

### 4. 实现视图

```csharp
public class PlayerView : View<PlayerState>
{
    [SerializeField] private Text levelText;
    [SerializeField] private Text expText;
    [SerializeField] private Button attackButton;
    
    public void OnEnable()
    {
        attackButton.onClick.AddListener(() =>
        {
            SendIntent(new GainExperienceIntent { Amount = 10 });
        });
    }
    
    protected override void OnStateChanged(PlayerState state)
    {
        levelText.text = state.Level.ToString();
        expText.text = state.Experience.ToString();
    }
}
```

### 5. 实现系统

```csharp
public class PlayerSystem : System
{
    private PlayerModel model;
    private PlayerView view;
    
    public override void Initialize()
    {
        model = new PlayerModel();
        view = GetView<PlayerView>();
        view.BindModel(model);
    }
    
    public override void ProcessIntent(IIntent intent)
    {
        if (intent is GainExperienceIntent gxIntent)
        {
            model.GainExperience(gxIntent.Amount);
        }
    }
}
```

---

## 当前状态

| 文件 | 状态 | 说明 |
|------|------|------|
| Module.cs | ✅ 活跃 | Module 基类与 IModule 接口 |
| State.cs | ✅ 活跃 | IState 基类 |
| Intent.cs | ✅ 活跃 | IIntent 接口 |
| Model.cs | 📝 注释 | 模型实现（架构模板） |
| View.cs | 📝 注释 | 视图实现（架构模板） |
| System.cs | 📝 注释 | 系统实现（架构模板） |
| App.cs | 📝 注释 | 应用容器（架构模板） |
| Example.cs | 📝 注释 | 完整示例与文档 |

---

## 激活与定制

若需在项目中使用 MVI 架构：

1. **参考 Example.cs**：查看完整的架构示例与实现模式
2. **取消注释**：根据需求在 Model.cs、View.cs、System.cs、App.cs 中取消注释
3. **定制实现**：根据具体业务逻辑扩展与修改
4. **集成应用**：将 MVI 系统注册到 App 并启动

---

## 与 Panel 系统的关系

- **Panel**（栈式导航）：管理 UI 面板的显示/隐藏、导航流程
- **MVI**（响应式架构）：管理单个面板内部的状态、数据流、UI 更新

**结合使用**：
- BasePanel 作为 MVI System 的容器
- 每个面板通过 MVI 架构管理内部复杂状态
- Panel 系统处理整体 UI 流程

---

## 最佳实践

1. **保持状态不可变**：每次更新创建新状态对象而非修改原对象
2. **单向数据流**：严格按照 Intent → Model → State → View 的流向
3. **模型独立**：Model 不依赖 View，便于单元测试
4. **细粒度订阅**：View 只订阅所需状态字段的变化以优化性能
5. **异步处理**：Intent 处理器中使用 async/await 处理异步操作

---

## 相关文档

- [UI/Panel 栈式导航系统](../Panel/README.md)
- [Events 事件系统](../../../Utility/Events/README.md)
