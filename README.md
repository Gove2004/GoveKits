# GoveKits

Unity Runtime Framework (com.gove.kits, v2.0.0)

本文档是主开发文档，按“命名空间 = 模块”组织，并且每个模块都提供代码示例。

## 1. 项目定位

GoveKits 聚焦 Unity 运行时基础设施：

- Core: 日志、事件总线、对象池、单例、全局生命周期。
- Storage: 配置读取、资源加载、存档读写、偏好设置。
- Unit: Attribute / Mark / Ability / Reaction 四容器协同。
- UI: Panel 栈式导航 + MVVM 基座。
- AI: FSM 与扩展占位。
- Network: TCP 传输、消息分发、RPC、Web API。
- Util: 定时器、本地化、音频、Reactive、ECS。
- Editor: 运行时调试器与项目工具。

## 2. 命名空间模块总览

| 命名空间 | 模块定位 | 典型入口 |
|---|---|---|
| GoveKits.Runtime.Core | 核心生命周期与日志 | GoveKitsCoreLifecycle, LogCore |
| GoveKits.Runtime.Core.Singleton | 单例基础设施 | MonoSingleton<T>, CSharpSingleton<T> |
| GoveKits.Runtime.Core.Event | 事件系统 | EventCore, EventBus, EventInfo |
| GoveKits.Runtime.Core.Pool | 对象池系统 | PoolCore, CSharpPool<T>, GameObjectPool |
| GoveKits.Runtime.Storage.Config | 配置系统 | ConfigCore, ConfigAttribute |
| GoveKits.Runtime.Storage.Res | 资源系统 | ResCore, IResLoader |
| GoveKits.Runtime.Storage.Save | 存档系统 | SaveCore, PrefsCore, AutoSaveBehaviour |
| GoveKits.Runtime.Unit | Unit 域模型 | IUnit, UnitBase, UnitCenter |
| GoveKits.Runtime.UI.Panel | 面板系统 | UIController, BasePanel |
| GoveKits.Runtime.UI.MVVM | MVVM 基座 | Model, ViewModel, View |
| GoveKits.Runtime.AI | AI 占位层 | Blackboard |
| GoveKits.Runtime.AI.FSM | FSM | FSM<,>, BaseState<,> |
| GoveKits.Runtime.Network.Protocol | 协议网络层 | NetworkManager, MessageDispatcher, RpcManager |
| GoveKits.Runtime.Util | 通用工具层 | TimerManager, AudioCore, LocalizationCore |
| GoveKits.Runtime.Util.ECS | 轻量 ECS | World, Entity, Filter |
| GoveKits.Editor | 项目工具窗口 | ProjectEditor |
| GoveKits.Editor.Project | 编辑器终端窗口 | TerminalWindow |
| GoveKits.Utility | 编辑器命令辅助 | Terminal |
| GoveKits.Editor.Save | 存档调试窗口 | SaveExplorerWindow, PrefsManagerWindow |
| GoveKits.Editor.Storage.Config | 配置调试窗口 | ConfigManagerWindow |
| GoveKits.Editor.Storage.Res | 资源监控窗口 | ResMonitorWindow |
| GoveKits.Unit.Editor | Unit Inspector 扩展 | UnitBehaviourEditor |
| Generated | Protobuf 生成消息 | BuiltinMsgId, RpcRequest, RpcResponse |

## 3. 启动时序

推荐启动顺序：

1. 场景创建 Boot 对象。
2. 挂载 GoveKitsCoreLifecycle（初始化 Timer 与 Config）。
3. 显式调用 UnitCenter.Initialize()。
4. 需要网络时挂载 NetworkManager。
5. 需要自动存档时挂载 AutoSaveBehaviour。
6. 需要 UI 栈时挂载 UIController 并配置面板数组。

最小启动代码：

```csharp
using UnityEngine;
using GoveKits.Runtime.Unit;

public sealed class GlobalBoot : MonoBehaviour
{
    private void Awake()
    {
        UnitCenter.Initialize();
    }
}
```

## 4. Runtime 模块（逐模块 + 示例）

### 4.1 GoveKits.Runtime.Core

职责：

- 全局生命周期挂点。
- Timer 驱动与 Config 启动。
- 日志输出。

代码示例：

```csharp
using UnityEngine;
using GoveKits.Runtime.Core;

public sealed class CoreBootExample : MonoBehaviour
{
    private void Awake()
    {
        LogCore.Log("CoreBootExample", "Boot start");
    }
}
```

### 4.2 GoveKits.Runtime.Core.Singleton

职责：

- 提供 Mono 与纯 C# 两种单例模型。

代码示例：

```csharp
using GoveKits.Runtime.Core.Singleton;
using UnityEngine;

public sealed class AudioService : MonoSingleton<AudioService>
{
    public void PlayClick() { }
}

public sealed class MatchConfig : CSharpSingleton<MatchConfig>
{
    public int MaxPlayer = 4;
}
```

### 4.3 GoveKits.Runtime.Core.Event

职责：

- 多总线事件发布订阅。
- 事件对象池化复用。

代码示例：

```csharp
using GoveKits.Runtime.Core.Event;

public sealed class DamageEvent : EventInfo
{
    public int Value;
    public override void OnRecycle() => Value = 0;
}

public sealed class EventUsage
{
    private DisposeAction _sub;

    public void Bind()
    {
        _sub = EventCore.Subscribe<DamageEvent>(e => UnityEngine.Debug.Log($"Damage={e.Value}"));
    }

    public void Fire()
    {
        EventCore.Publish<DamageEvent>(e => e.Value = 100);
    }

    public void Unbind()
    {
        _sub?.Dispose();
    }
}
```

### 4.4 GoveKits.Runtime.Core.Pool

职责：

- C# 对象池。
- GameObject 对象池。

代码示例：

```csharp
using GoveKits.Runtime.Core.Pool;
using UnityEngine;

public sealed class MyBulletData : IPoolable
{
    public float Speed;
    public void OnRecycle() => Speed = 0;
}

public sealed class PoolUsage
{
    public void Use(GameObject bulletPrefab)
    {
        var data = PoolCore.Get<MyBulletData>();
        data.Speed = 20;
        PoolCore.Return(data);

        GameObject bullet = PoolCore.Get(bulletPrefab);
        PoolCore.Return(bullet);
    }
}
```

### 4.5 GoveKits.Runtime.Storage.Config

职责：

- 扫描 ConfigAttribute 并装载配置。

代码示例：

```csharp
using GoveKits.Runtime.Storage.Config;
using Cysharp.Threading.Tasks;

[Config("Config/Hero", ConfigFileType.Json, ConfigSourceType.Resources)]
public sealed class HeroCfg : IConfigData
{
    public int Id;
    public string Name;
}

public static class ConfigUsage
{
    public static async UniTask InitAndRead()
    {
        await ConfigCore.InitAsync();
        var heroes = ConfigCore.LoadAll<HeroCfg>();
        var tanks = ConfigCore.Load<HeroCfg>(x => x.Name.Contains("Tank"));
    }
}
```

### 4.6 GoveKits.Runtime.Storage.Res

职责：

- 统一 Resources/AssetBundle/Addressables 加载与释放。

代码示例：

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Res;

public static class ResUsage
{
    public static async UniTask LoadAudio()
    {
        var clip = await ResCore.LoadAsync<AudioClip>(ResLoadType.Resources, "Audio/BGM/Main");
        // use clip
        ResCore.Release<AudioClip>(ResLoadType.Resources, "Audio/BGM/Main");
    }
}
```

### 4.7 GoveKits.Runtime.Storage.Save

职责：

- 存档序列化与原子写入。

代码示例：

```csharp
using GoveKits.Runtime.Storage.Save;
using Cysharp.Threading.Tasks;

public sealed class PlayerSaveData : ISaveData<PlayerSnapshot>
{
    public string RelativePath => "profile/player";
    public PlayerSnapshot Save() => new PlayerSnapshot { Level = 5 };
    public void Load(PlayerSnapshot data) { }
}

public sealed class PlayerSnapshot
{
    public int Level;
}

public static class SaveUsage
{
    public static async UniTask SaveAndLoad(PlayerSaveData save)
    {
        SaveCore.CurrentFormat = SerializerType.Json;
        await SaveCore.SaveAsync(save);
        await SaveCore.LoadAsync(save);
    }
}
```

### 4.8 GoveKits.Runtime.Unit

职责：

- Unit 四容器协同。
- Ability/Mark/Reaction 工厂注册。

代码示例：

```csharp
using GoveKits.Runtime.Unit;
using Cysharp.Threading.Tasks;

[AutoUnit]
public sealed class SlashAbility : UnitAbility
{
    public static readonly UnitTag Tag = "Ability.Slash";
    public override UnitTag Name => Tag;

    public SlashAbility(IUnit owner) : base(owner) { }

    public override UniTask ExecuteAsync(UnitContext context)
    {
        // deal damage
        return UniTask.CompletedTask;
    }
}

public sealed class HeroUnit : UnitBase
{
    public override void InitAttributes() { }
    public override void InitMarks() { }
    public override void InitAbilities() { }
    public override void InitReactions() { }
}
```

### 4.9 GoveKits.Runtime.UI.Panel

职责：

- Panel 栈式导航与生命周期。

代码示例：

```csharp
using UnityEngine;
using GoveKits.Runtime.UI.Panel;

public sealed class UiBoot : MonoBehaviour
{
    [SerializeField] private UIController controller;

    private void Start()
    {
        controller.Show<MainPanel>(new { UserId = 1001 });
    }
}

public sealed class MainPanel : BasePanel { }
```

### 4.10 GoveKits.Runtime.UI.MVVM

职责：

- Model / ViewModel / View 组织。
- RelayCommand 命令。

代码示例：

```csharp
using GoveKits.Runtime.UI.MVVM;

public sealed class LoginModel : Model
{
    public string UserName;
}

public sealed class LoginViewModel : ViewModel<LoginModel>
{
    public ICommand SubmitCommand { get; }

    public LoginViewModel()
    {
        SubmitCommand = new RelayCommand(() => { });
    }
}
```

### 4.11 GoveKits.Runtime.AI

职责：

- Blackboard 占位层，待行为树/GOAP 扩展。

代码示例：

```csharp
using GoveKits.Runtime.AI;

public sealed class AiAgentContext
{
    public Blackboard Blackboard = new Blackboard();
}
```

### 4.12 GoveKits.Runtime.AI.FSM

职责：

- 泛型 FSM 与异步状态切换。

代码示例：

```csharp
using GoveKits.Runtime.AI.FSM;
using Cysharp.Threading.Tasks;

public enum BotState { Idle, Chase }

public sealed class Bot : IFSMObject
{
    public FSM<BotState, Bot> Fsm;

    public void Init()
    {
        Fsm = new FSM<BotState, Bot>(this);
        Fsm.AddState(BotState.Idle, new IdleState());
        Fsm.AddState(BotState.Chase, new ChaseState());
        Fsm.Start(BotState.Idle);
    }
}

public sealed class IdleState : BaseState<BotState, Bot> { }
public sealed class ChaseState : BaseState<BotState, Bot> { }
```

### 4.13 GoveKits.Runtime.Network.Protocol

职责：

- 网络连接、消息注册分发、RPC。

代码示例：

```csharp
using UnityEngine;
using GoveKits.Runtime.Network.Protocol;
using Cysharp.Threading.Tasks;
using Generated;

public sealed class NetworkBoot : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        NetworkManager.Instance.Connect("127.0.0.1", 2233);

        var req = new LoginReq { Username = "u", Password = "p" };
        LoginResp rsp = await RpcManager.Instance.Call<LoginResp>(req, timeoutMs: 5000);

        NetworkManager.Instance.Disconnect();
    }
}
```

### 4.14 GoveKits.Runtime.Util

职责：

- Timer / Audio / Localization / Reactive 等通用能力。

代码示例：

```csharp
using UnityEngine;
using GoveKits.Runtime.Util;

public sealed class UtilUsage : MonoBehaviour
{
    [SerializeField] private AudioConfig uiClick;

    private void Start()
    {
        TimerManager.Initialize();
        TimerManager.Once(1f, () => Debug.Log("tick"));

        LocalizationCore.Initialize();
        Debug.Log(LocalizationCore.GetText("ui.start"));

        AudioCore.Init();
        AudioCore.Play(uiClick);
        AudioCore.PlayBGM("Audio/BGM/Main", fadeTime: 0.5f);

        var hp = Reactive.Int(100);
        hp.Watch(() => Debug.Log($"HP={hp.Value}"));
        hp.Value -= 10;
    }

    private void Update()
    {
        TimerManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) AudioCore.PauseAll();
        else AudioCore.ResumeAll();
    }

    private void OnDestroy()
    {
        AudioCore.StopBGM();
    }
}
```

### 4.15 GoveKits.Runtime.Util.ECS

职责：

- World / Entity / ComponentPool / Filter / SystemGroup。

代码示例：

```csharp
using GoveKits.Runtime.Util.ECS;

public struct Position
{
    public float X;
    public float Y;
}

public static class EcsUsage
{
    public static void Run()
    {
        var world = new World();
        var e = world.CreateEntity();

        world.AddComponent(e, new Position { X = 1, Y = 2 });

        if (world.HasComponent<Position>(e))
        {
            Position p = world.GetComponent<Position>(e);
            p.X += 1;
            world.AddComponent(e, p);
        }
    }
}
```

## 5. Editor 模块（逐模块 + 示例）

### 5.1 GoveKits.Editor

职责：

- 项目辅助窗口（如 .gitignore 初始化、Android 隐私弹窗复制）。

代码示例（打开窗口）：

```csharp
using UnityEditor;
using GoveKits.Editor;

public static class OpenProjectEditorExample
{
    [MenuItem("Tools/Open Project Editor")]
    private static void Open()
    {
        ProjectEditor.ShowWindow();
    }
}
```

### 5.2 GoveKits.Editor.Project

职责：

- 终端编辑器窗口。

代码示例（菜单路径）：

```text
Unity Menu -> GoveKits/Project
```

### 5.3 GoveKits.Utility

职责：

- 编辑器命令执行辅助。

代码示例（示意）：

```csharp
// 该命名空间为编辑器辅助工具，可在 Editor 扩展中调用其命令执行能力。
```

### 5.4 GoveKits.Editor.Save

职责：

- SaveExplorer 与 Prefs 管理。

代码示例（打开窗口）：

```csharp
using UnityEditor;

public static class OpenSaveToolsExample
{
    [MenuItem("Tools/Open Save Explorer")]
    private static void OpenSaveExplorer()
    {
        GoveKits.Editor.Save.SaveExplorerWindow.ShowWindow();
    }
}
```

### 5.5 GoveKits.Editor.Storage.Config

职责：

- 配置扫描、初始化、体检、报告复制。

代码示例（菜单路径）：

```text
Unity Menu -> GoveKits/Storage/Config Manager
```

### 5.6 GoveKits.Editor.Storage.Res

职责：

- 缓存资源监控与释放。

代码示例（菜单路径）：

```text
Unity Menu -> GoveKits/Storage/Res Monitor
```

### 5.7 GoveKits.Unit.Editor

职责：

- UnitBehaviour 自定义 Inspector。

代码示例（用途）：

```text
Play Mode 下选中 UnitBehaviour 对象，可查看 Attributes/Marks/Abilities/Reactions 状态。
```

## 6. Generated 模块（逐模块 + 示例）

职责：

- Protobuf 自动生成消息定义，不建议手改。

代码示例：

```csharp
using Generated;
using Google.Protobuf;

public static class GeneratedUsage
{
    public static RpcRequest BuildRpc(int id, int targetMsgId, IMessage body)
    {
        return new RpcRequest
        {
            RpcId = id,
            TargetMsgId = targetMsgId,
            Payload = ByteString.CopyFrom(body.ToByteArray())
        };
    }
}
```

## 7. 排障清单

1. 生命周期：是否重复驱动 TimerManager.Update。
2. 事件：是否忘记 DisposeAction。
3. 对象池：是否把非池对象 Return。
4. 资源：Load 后是否 Release。
5. 存档：SerializerType 是否与历史数据一致。
6. 网络：MessageRegistry 映射与 Rpc 超时处理是否正确。

## 8. 验收标准

1. 每个新增模块都要有：职责、入口、示例。
2. 每个新增 API 都要有最小调用样例。
3. 关键生命周期要有初始化与释放路径。
4. README 只维护在本文件。
