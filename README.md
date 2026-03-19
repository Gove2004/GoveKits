# GoveKits

Unity Runtime Framework (com.gove.kits, v2.0.0)

本 README 是项目唯一主文档。仓库中的子目录 README 已清理，后续文档请统一维护于此文件。

## 1. 框架概览 (The Core Concept)

GoveKits 的核心目标是把 Unity 项目中的高频基础能力抽象为可组合的运行时模块，并给出统一的初始化、通信、资源与调试路径，减少业务代码对 Unity 细节 API 的直接耦合。

它主要解决了以下原生开发痛点：

- 生命周期分散：不同系统自行 Awake/Start/Update，初始化顺序难控。
- 对象创建与销毁成本高：高频短生命周期对象造成 GC 抖动。
- 组件通信混乱：跨模块调用依赖具体引用，难以维护。
- 资源与配置读取方式不统一：Resources/AB/Addressables、Json/Csv/Proto 混用。
- 运行时可观测性不足：问题定位依赖 Debug.Log，缺少系统级视图。

GoveKits 的架构方法：

- 统一入口：Core 静态门面 + Lifecycle 组件。
- 解耦通信：事件总线 EventCore + 类型化监听。
- 低 GC 策略：PoolCore 贯穿 Event/Effect/Timer 等短对象。
- 组合式 Unit 模型：Attribute + Mark + Ability + Reaction 四容器协同。
- 编辑器可观测：Pool/Event/Save/Config/Res/Unit 运行时面板。

## 2. 目录结构与模块说明 (Structure)

### 2.1 目录树

```text
GoveKits
|- Assets/
|  |- GoveKits/
|  |  |- Runtime/
|  |  |  |- Core/              # 日志/事件/对象池/单例/核心生命周期
|  |  |  |- Storage/           # 配置/资源/存档
|  |  |  |- Unit/              # 属性/标记/技能/反应系统
|  |  |  |- UI/                # Panel 栈与 MVVM 基座
|  |  |  |- AI/                # FSM
|  |  |  |- Network/           # 协议层与 Web API
|  |  |  |- Util/              # 时间轮/本地化/音频/Reactive/ECS
|  |  |- Editor/
|  |  |  |- Core/              # Pool/Event 调试器
|  |  |  |- Storage/           # Config/Res/Save 调试器
|  |  |  |- Save/              # Prefs/Save 可视化
|  |  |  |- Unit/              # Unit 运行时 Inspector
|  |  |  |- Terminal/          # 编辑器终端扩展
|  |  |  |- Project/           # 项目辅助工具
|  |  |- Plugins/              # 第三方插件入口
|- ProjectSettings/
|- Packages/
|- README.md                   # 本文档
```

### 2.2 模块分类

核心模块（Framework Core）：

- Runtime/Core
- Runtime/Storage
- Runtime/Unit
- Runtime/UI
- Runtime/AI/FSM
- Runtime/Network/Protocol 与 Runtime/Network/API
- Runtime/Util
- Editor 调试工具链

业务支撑模块（Gameplay-facing）：

- Runtime/Unit/Extension（CDRule、Attribute/Mark/Ability Effect）
- Runtime/UI/Panel（业务界面调度）
- Runtime/Network/Protocol/Utility（AutoConnection、Heartbeat、Discovery）

说明：当前仓库中未包含具体游戏业务（关卡、战斗策划数据、具体职业技能等）实现，现有代码主要是框架层与可复用中间层。

## 3. 模块深度解析 (Module Deep Dive)

### 3.1 Core 模块 (Runtime/Core)

模块职责：

- 提供统一日志输出 LogCore。
- 提供事件总线 EventCore/EventBus/EventInfo。
- 提供对象池 PoolCore（C# 对象池 + GameObject 池）。
- 提供单例基类 MonoSingleton 与 CSharpSingleton。
- 通过 GoveKitsCoreLifecycle 挂接全局初始化时机。

设计原理：

- 事件对象池化：Publish 时从池取 EventInfo，分发后 finally 回收，降低临时分配。
- 多总线隔离：EventCore 支持 busName，可按子系统隔离事件域。
- 优先级监听：EventChannel 在 dirty 时排序，Priority 越大越先执行。
- 事件快照派发：频道分发时复制监听快照，避免遍历中增删监听器导致异常。
- 双通道对象池：Type 维度管理 C# 对象，Prefab ID 维度管理 GameObject。

交互接口：

- EventCore.Publish<T>(Action<T> init, string busName = "main")
- EventCore.Subscribe<T>(Action<T> callback, int priority = 0, string busName = "main")
- PoolCore.Get<T>() / Return<T>(T item) / Get(GameObject prefab) / Return(GameObject obj)
- MonoSingleton<T>.Instance / CSharpSingleton<T>.Instance

示例代码：

```csharp
using GoveKits.Runtime.Core.Event;
using GoveKits.Runtime.Core.Pool;

public sealed class DamageEvent : EventInfo
{
    public int Value;
    public override void OnRecycle() => Value = 0;
}

public class Demo
{
    private DisposeAction _sub;

    public void Bind()
    {
        _sub = EventCore.Subscribe<DamageEvent>(e => UnityEngine.Debug.Log(e.Value), priority: 10);
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

注意事项：

- EventInfo 派生类必须在 OnRecycle 中清理字段，否则池化复用会出现脏数据。
- 监听句柄必须 Dispose；否则跨场景残留监听会导致幽灵回调。
- GameObject 回池依赖 PoolRecord + SourcePool，不可将非池对象直接 Return。
- MonoSingleton 在应用退出阶段会返回 null，业务侧需做空判。

### 3.2 Storage 模块 (Runtime/Storage)

模块职责：

- ConfigCore：按 ConfigAttribute 扫描并加载配置表。
- ResCore：统一 Resources/AssetBundle/Addressables 三类加载方式。
- SaveCore：统一 Json/Protobuf 序列化存档，支持同步与异步。
- PrefsCore：PlayerPrefs 最小封装。
- AutoSaveBehaviour：定时批量保存注册目标。

设计原理：

- 配置反射扫描：ConfigBindingScanner 扫描 IConfigData + ConfigAttribute。
- 解析器可插拔：IConfigParser + ConfigFileType 映射。
- 资源引用计数：CacheContainer 引用计数归零时触发卸载。
- 存档原子写：先写 temp，再 File.Replace/Move，降低写盘中断损坏风险。

交互接口：

- ConfigCore.InitAsync()/LoadAll<T>()/Load<T>(predicate)
- ResCore.Load<T>()/LoadAsync<T>()/Release<T>()
- SaveCore.Save/Load/SaveAsync/LoadAsync/RegisterSerializer
- AutoSaveBehaviour.Register/Unregister/SaveAllAsync

示例代码：

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Config;
using GoveKits.Runtime.Storage.Res;
using UnityEngine;

public class StorageBoot : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        await ConfigCore.InitAsync();
        var clip = await ResCore.LoadAsync<AudioClip>(ResLoadType.Resources, "Audio/BGM/Main");
        // ... use clip
        ResCore.Release<AudioClip>(ResLoadType.Resources, "Audio/BGM/Main");
    }
}
```

注意事项：

- ConfigCore 在未 InitAsync 前调用 Load 系列会抛异常。
- AddressablesResLoader 依赖 UNITASK_ADDRESSABLE_SUPPORT 编译宏。
- SaveCore.CurrentFormat 必须与历史存档格式一致，否则反序列化失败。
- AutoSaveBehaviour 是 MonoSingleton，建议仅保留一个实例。

### 3.3 Unit 模块 (Runtime/Unit)

模块职责：

- IUnit/UnitBase/UnitBehaviour 定义 Unit 统一行为。
- AttributeContainer 管理 StateAttribute/RuntimeAttribute。
- MarkContainer 管理持续状态与叠层。
- AbilityContainer 管理技能执行。
- ReactionContainer 基于事件系统实现被动响应。
- UnitCenter + AbilityCenter/MarkCenter/ReactionCenter 扫描并注册工厂。

设计原理：

- 四容器分治：把属性、状态、主动技能、被动反应拆为正交子系统。
- Tag 轻量键：UnitTag 预计算哈希，提高字典查询效率。
- Ability 规则链：CanExecute + Commit + ExecuteAsync 形成执行前检查与副作用提交。
- Reaction 订阅封装：UnitReaction<T> 自动管理 EventCore 订阅/反订阅。
- 工厂反射创建：带 AutoUnitAttribute 的类型按构造签名自动实例化。

交互接口：

- UnitCenter.Initialize()
- AbilityCenter.Create<TAbility>(owner, args)
- MarkCenter.Create<TMark>(owner, args)
- ReactionCenter.Create<TReaction>(owner, args)
- IUnit.Use(UnitTag, UnitContext)

示例代码：

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Unit;

public class Hero : UnitBehaviour
{
    public static readonly UnitTag ATK = "Attr.ATK";
    public static readonly UnitTag HP_MAX = "Attr.HP.Max";
    public static readonly UnitTag HP_CUR = "Attr.HP.Cur";

    public override void InitAttributes()
    {
        var hpMax = Attributes.AddState(HP_MAX, 100, 0, 99999);
        Attributes.AddRuntime(HP_CUR, hpMax);
        Attributes.AddState(ATK, 20);
    }

    public override void InitMarks() {}
    public override void InitAbilities() {}
    public override void InitReactions() {}
}
```

注意事项：

- UnitAbility 已提供 AddRule/RemoveRule/ClearRules，建议在业务能力构造时集中注册规则，避免运行中动态改规则导致行为不可预测。
- MarkContainer.UpdateMarks 需要在驱动层每帧调用（UnitBehaviour 已默认调用）。
- AbilityCenter/MarkCenter/ReactionCenter 已统一为 AutoUnitAttribute 文案，若出现未注册异常请优先检查特性声明与构造签名。
- 当前仓库没有 AutoUnitAttribute 的具体业务子类示例，建议补充并加注释。

### 3.4 UI 模块 (Runtime/UI)

模块职责：

- Panel 子系统：UIController + BasePanel + IPanelLifeCycle。
- MVVM 子系统：Model/ViewModel/View 基类与命令系统 RelayCommand。
- Panel 生命周期对外通过 PanelEvent 事件广播。

设计原理：

- 栈式导航：UIController 用 Stack<BasePanel> 管理前后台与弹窗叠层。
- 生命周期约束：BasePanel 通过显式接口实现，外部无法随意跳过状态流转。
- 数据驱动刷新：ViewModel.PropertyChanged 推送，View 局部刷新。

交互接口：

- UIController.Show<T>(payload)/Hide()/FinishAll()/GetPanel<T>()
- BasePanel.OnCreate/OnStart/OnResume/OnPause/OnStop/OnFinish
- View<TViewModel>.SetViewModel(...)

示例代码：

```csharp
using GoveKits.Runtime.UI.Panel;

public class UIBoot : UnityEngine.MonoBehaviour
{
    public UIController Controller;

    void Start()
    {
        Controller.Show<MainPanel>(new { UserId = 1 });
    }
}
```

注意事项：

- 目前 Panel 生命周期方法是同步调用，若要做动画 await，请在派生类内自行封装异步流程。
- BasePanel 默认会发布 PanelEvent，监听方应及时释放订阅。

### 3.5 AI 模块 (Runtime/AI/FSM)

模块职责：

- 泛型 FSM<TStateEnum, TOwner> 支持状态注册、切换、Update/FixedUpdate 驱动。
- BaseState 提供 OnEnter/OnExit 异步生命周期。

设计原理：

- 切换互斥：_isTransitioning 防重入，避免状态切换嵌套污染。
- Owner 约束：IFSMObject 限制持有者类型，保证状态内 Owner 访问安全。

交互接口：

- FSM.AddState(...)/Start(...)/ChangeState(...)/Update()/FixedUpdate()
- BaseState.ChangeState(next)

示例代码：

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.AI.FSM;

public enum BotState { Idle, Chase }
public class Bot : IFSMObject
{
    public FSM<BotState, Bot> Fsm;

    public void InitFSM()
    {
        Fsm = new FSM<BotState, Bot>(this);
        Fsm.AddState(BotState.Idle, new IdleState());
        Fsm.AddState(BotState.Chase, new ChaseState());
        Fsm.Start(BotState.Idle);
    }
}

public sealed class IdleState : BaseState<BotState, Bot> {}
public sealed class ChaseState : BaseState<BotState, Bot> {}
```

注意事项：

- FSM.Start 内部调用 OnEnter().Forget()，如需严格串行初始化，请在外层等待自定义启动流程。
- Blackboard.cs 当前为占位类型，建议在开始行为树/GOAP 接入时补充键值读写、作用域与线程模型说明。

### 3.6 Network 模块 (Runtime/Network)

模块职责：

- Protocol/Core：NetworkManager、NetworkClient、TcpTransport、PacketParser、MessageRegistry、MessageDispatcher。
- Protocol/Rpc：RpcManager 请求-应答封装。
- Protocol/Utility：AutoConnection、HeartbeatComponent、NetworkDiscovery。
- API：WebAPI + RequestData + ResponseData。

设计原理：

- 消息注册：MessageRegistry.ScanAndRegister<TEnum> 通过枚举值映射 Protobuf Parser。
- 主线程分发：MessageDispatcher.DispatchAsync 会切回主线程，保证 Unity API 安全。
- 传输层隔离：ITransport 抽象连接，当前默认 TcpTransport。
- 增量解包：PacketParser 用读写索引处理粘包半包。

交互接口：

- NetworkManager.Connect/Disconnect/Send
- MessageDispatcher.Bind/Unbind + MessageHandlerAttribute
- RpcManager.Call<TResponse>(request)
- WebAPI.Send(RequestData)

示例代码：

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Network.Protocol;
using Generated;

public class NetBoot : UnityEngine.MonoBehaviour
{
    async UniTaskVoid Start()
    {
        NetworkManager.Instance.Connect("127.0.0.1", 12345);
        var req = new RpcRequest { RpcId = 1, TargetMsgId = 1001 };
        var rsp = await RpcManager.Instance.Call<RpcResponse>(req);
    }
}
```

注意事项：

- Peer 目录中的 NetworkIdentity/NetworkBehaviour/SpawnerManager 当前为整段注释状态，尚非可用功能。
- RpcManager 内存在临时打包冗余变量与反射 TrySetResult 路径，建议补注释说明并评估性能。
- MessageRegistry 依赖枚举名与消息类名约定，改名需同步。

### 3.7 Util 模块 (Runtime/Util)

模块职责：

- Time：TimerManager + TimeWheel + Timer。
- Audio：AudioCore 统一音频通道与偏好持久化。
- Localization：LocalizationCore + LocalizationComponent。
- Reactive：Ref<T> 响应式变量。
- ECS：World/Filter/SystemGroup 稀疏集 ECS。

设计原理：

- 时间轮：固定 tick 槽位调度，适合大量定时任务。
- 本地化缓存：RawRows -> CurrentLangCache，语言切换后统一刷新。
- 响应式链路：Ref 的 Watch + DependOn 建立传播图。
- ECS 稀疏集：ComponentPool<T> 采用 sparse + dense 结构 O(1) 增删查。

交互接口：

- TimerManager.Once/Loop/Cancel/ClearAll
- AudioCore.Init/PlayBGM/PlaySFX/SetVolume
- LocalizationCore.Initialize/SwitchLanguage/GetText
- Reactive.Int/Float/String/Bool
- World.CreateEntity/AddComponent/GetFilter

示例代码：

```csharp
using GoveKits.Runtime.Util;

public class UtilDemo : UnityEngine.MonoBehaviour
{
    void Start()
    {
        TimerManager.Initialize();
        TimerManager.Once(1.0f, () => UnityEngine.Debug.Log("tick"));

        LocalizationCore.Initialize();
        UnityEngine.Debug.Log(LocalizationCore.GetText("ui.start"));
    }

    void Update()
    {
        TimerManager.Update(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);
    }
}
```

注意事项：

- 若场景中挂载 GoveKitsCoreLifecycle，TimerManager 已由其 Update 默认驱动；如果项目自定义生命周期，请确保只保留一个驱动入口。
- AudioCore 使用 _root.GetComponent<MonoBehaviour>() 启动协程，运行依赖场景中可用 MonoBehaviour 组件，建议补专用 driver 组件并写注释。
- ECS 文件 Etity.cs 存在命名拼写问题，建议改为 Entity.cs 并补迁移说明。

### 3.8 Editor 模块 (Assets/GoveKits/Editor)

模块职责：

- Core：EventDebuggerWindow、PoolDebuggerWindow。
- Storage：ConfigManagerWindow、ResMonitorWindow、SaveExplorerWindow。
- Unit：UnitBehaviourEditor 运行时属性/技能/状态可视化。
- Project：.gitignore 初始化工具。

设计原理：

- 运行时系统暴露 Debug API（例如 EventCore.GetDebugBusNames、PoolCore.GetDebugCSharpPools）。
- 编辑器窗口通过轮询 + 事件刷新实现低侵入可观测。

交互接口：

- 菜单路径统一在 GoveKits/... 下。

示例代码：

```text
Unity 菜单:
GoveKits/Core/Event Debugger
GoveKits/Core/Pool Debugger
GoveKits/Storage/Config Manager
GoveKits/Storage/Res Monitor
GoveKits/Storage/Save Explorer
```

注意事项：

- 大部分 Editor 能力依赖 Play Mode 下 Runtime 状态；非运行时数据为空属于预期。

## 4. 框架集成与初始化 (Integration)

### 4.1 快速集成步骤

1. 导入 Assets/GoveKits 到新项目（或以 UPM 包形式导入 com.gove.kits）。
2. 确认第三方依赖可用：
   - Cysharp.Threading.Tasks (UniTask)
   - Newtonsoft.Json
   - Google.Protobuf
   - Addressables（可选，若使用 AddressablesResLoader）
3. 在启动场景创建 GlobalBoot 对象并挂载：
   - GoveKitsCoreLifecycle（触发 ConfigCore.InitAsync）
   - 可选 NetworkManager、AutoSaveBehaviour、UIController
4. 在任意早期初始化点调用 UnitCenter.Initialize()（虽有自动初始化，但显式调用更可控）。
5. 若使用 TimerManager，新增一个 Driver 在 Update 中转发 TimerManager.Update。

### 4.2 推荐启动脚本

```csharp
using UnityEngine;
using GoveKits.Runtime.Unit;
using GoveKits.Runtime.Util;

public sealed class GlobalBoot : MonoBehaviour
{
    void Awake()
    {
        UnitCenter.Initialize();
        TimerManager.Initialize();
    }

    void Update()
    {
        TimerManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }
}
```

### 4.3 需要挂载的关键对象

- 必选：至少一个承载启动逻辑的 Boot 物体。
- 推荐：
  - GoveKitsCoreLifecycle
  - NetworkManager（需要网络时）
  - AutoSaveBehaviour（需要自动存档时）
  - UIController（使用 Panel 系统时）

## 5. 扩展指南 (Contribution & Extension)

### 5.1 新增子模块规范

1. 明确边界：模块只暴露门面 API，不直接泄漏内部集合与状态。
2. 遵循注册机制：
   - Unit 子类型使用 AutoUnitAttribute + Center.Create。
   - 事件通信统一通过 EventCore，不跨模块硬引用。
3. 生命周期可释放：
   - 监听器必须返回并保存 DisposeAction。
   - 持有外部资源必须在 Dispose/OnDestroy 中释放。
4. 性能约束：
   - 高频短对象优先接入 PoolCore。
   - 每帧路径避免反射与字符串拼接。
5. 可观测性要求：
   - 为核心状态提供最小调试出口（统计/快照）。

### 5.2 保证松耦合的方法

- 用事件替代直接引用：A 发布事件，B 订阅处理。
- 用接口替代具体类：例如 ITransport、IConfigParser、ISerializer。
- 用上下文对象传递态：UnitContext/RequestData，避免参数爆炸。
- 用模块门面替代散调用：统一经 Core/Center/Manager 入口访问。

### 5.3 代码注释补强建议

本次扫描发现以下区域仍应补注释或补全实现说明：

- Runtime/AI/Blackboard.cs 目前仅占位类型，缺少键空间、生命周期、并发访问规则的设计说明。
- Runtime/Network/Protocol/Peer 下多文件为整段注释代码，应标注弃用/待实现状态。
- UnitAbility 规则系统建议补“规则注册顺序与冲突策略”注释样例。
- AudioCore 协程驱动路径建议增加实现注释，避免后续维护者误以为可在纯静态上下文直接开协程。

---

## 附录：依赖与通信总览

关键依赖关系：

- UnitReaction 依赖 EventCore；UnitEffect/EventInfo/Timer 依赖 PoolCore。
- LocalizationCore 依赖 ConfigCore；AudioCore 依赖 ResCore + PrefsCore。
- AutoSaveBehaviour 依赖 SaveCore；Editor 调试面板依赖 Runtime Debug API。
- NetworkClient 依赖 ITransport + PacketParser + MessageDispatcher。

关键通信机制：

- 事件驱动：EventCore (发布/订阅/优先级/中断)
- 容器协同：Attribute-Mark-Ability-Reaction
- 请求响应：WebAPI / RpcManager
- 生命周期驱动：MonoBehaviour + Center 初始化 + Editor 观察

## 6. 深度架构补充 (Architecture In Practice)

### 6.1 启动时序与初始化边界

推荐启动时序：

1. Unity 进入首场景。
2. GoveKitsCoreLifecycle.Awake：
    - LogCore 输出启动日志。
    - TimerManager.Initialize。
    - ConfigCore.InitAsync 异步加载配置。
3. UnitCenter 自动初始化（BeforeSceneLoad），扫描 Ability/Mark/Reaction 工厂。
4. NetworkManager.Awake（如挂载）：注册协议映射并绑定消息分发。
5. 业务 Boot 脚本执行：加载首屏、绑定事件、拉起 UI。

初始化边界建议：

- ConfigCore：只在启动链路做一次全量 InitAsync，运行期不重复 Init。
- UnitCenter：支持自动初始化，但业务层建议显式调用一次以保证可读性。
- TimerManager：由生命周期脚本唯一驱动，不允许多个 Update 同时推进。
- Network：仅在需要联网场景挂载 NetworkManager，避免无谓套接字生命周期。

### 6.2 运行时主链路示意

战斗链路（主动技能）：

1. 输入层触发 AbilityTag。
2. AbilityContainer.TryExecuteAsync 命中能力。
3. UnitAbility.CanExecute 依次检查规则。
4. Rule.Commit 写入副作用（例如 CDMark）。
5. ExecuteAsync 执行效果：
    - 修改属性（Attribute）
    - 增减状态（Mark）
    - 发布事件（EventCore）
6. ReactionContainer 中激活的被动监听到事件后执行响应逻辑。

界面链路（Panel）：

1. UIController.Show 处理当前栈顶面板 Pause/Stop。
2. 新面板 OnCreate(首次) -> OnStart(payload) -> OnResume。
3. BasePanel 发布 PanelEvent 给观察者（埋点/日志/调试）。
4. Hide 时栈顶出栈，恢复下层面板 OnStart/OnResume。

存档链路：

1. AutoSaveBehaviour 定时触发 SaveAllAsync。
2. SaveCore.SaveAsync 调用 ISaveData.Save 导出状态。
3. Serializer 序列化后写入 temp 文件。
4. 原子替换正式存档文件，完成落盘。

### 6.3 依赖分层约束

建议遵循的依赖方向：

- 允许：业务模块 -> Runtime Core/Storage/Unit/UI/Util。
- 允许：Editor 工具 -> Runtime（仅读调试接口）。
- 禁止：Runtime -> Editor 命名空间引用。
- 禁止：Unit 子模块之间通过具体类强耦合互调（优先 EventCore 与容器接口）。
- 禁止：业务模块直接操作 Pool 内部结构（只能经 PoolCore 门面）。

跨模块通信优先级：

1. 事件总线（适合广播与解耦）。
2. 容器接口（适合同一 Unit 内部协作）。
3. 直接引用（仅在生命周期强一致且边界清晰时使用）。

### 6.4 性能设计与预算建议

对象分配策略：

- 高频临时对象：EventInfo、UnitEffect、Timer 必须池化。
- 中频对象：按业务压测结果决定是否池化，避免过度复杂化。
- 低频对象：优先可读性，必要时再优化。

关键热路径建议指标（参考）：

- 每帧 GC Alloc：战斗场景尽量稳定在 0 或低可控水平。
- Event Publish：避免在单帧内发布高频大对象事件。
- UI 刷新：PropertyChanged 只做必要字段刷新，不做全量重建。
- 网络分发：MessageDispatcher 回调中避免长耗时阻塞主线程。

容易引发抖动的点：

- 字符串频繁拼接日志。
- 未释放的事件订阅导致重复回调。
- ResCore 加载后未 Release 造成常驻内存膨胀。
- 规则链过长导致技能前置检查过重。

### 6.5 并发与线程边界

线程约束：

- Unity 对象访问必须在主线程。
- MessageDispatcher 已切回主线程再调用 handler。
- ConfigCore/SaveCore 的 IO 可异步执行，但回写业务状态要注意线程上下文。

取消与超时：

- 所有外部网络请求建议携带 CancellationToken。
- RPC 默认超时后要清理 pending 表，避免泄漏。
- 配置异步加载若允许中断，应对失败场景提供兜底启动策略。

### 6.6 可观测性与线上诊断建议

推荐长期保留的诊断能力：

- Event 历史（最近 N 条）与活跃订阅统计。
- Pool 缓存量、活跃量、上限阈值告警。
- Save 写入耗时与失败统计。
- Res 缓存条目与引用计数快照。

排障优先顺序：

1. 先查生命周期是否按预期执行。
2. 再查订阅是否存在与是否正确释放。
3. 再查对象池回收与复用状态。
4. 最后查业务逻辑分支与配置数据。

## 7. 扩展落地模板 (Practical Templates)

### 7.1 新增 Ability 的推荐步骤

1. 创建 Ability 类，定义唯一 UnitTag。
2. 在构造函数中 AddRule（CD、资源消耗、状态条件）。
3. ExecuteAsync 中只处理业务结果，不做重复前置检查。
4. 在 Unit.InitAbilities 中统一注册。
5. 为该能力补一条最小验证路径（手动触发 + 状态断言）。

### 7.2 新增 Mark 的推荐步骤

1. 明确是否可叠层、是否周期触发、持续时间模型。
2. 使用 UnitMark 或 TickMark 实现。
3. 在 OnApply/OnStack/OnUpdate/OnRemove 中保持幂等与可重复进入。
4. 若 Mark 依赖外部资源，必须在 OnRemove 释放。

### 7.3 新增网络消息的推荐步骤

1. 在 Proto 与枚举中同步增加消息定义。
2. 确保 MessageRegistry 能按命名约定扫描到 Parser。
3. 在目标处理类添加 MessageHandler 标记方法。
4. 压测高频消息场景，确认主线程回调无长阻塞。

## 8. 验收清单 (Definition of Done)

每次新增模块或大改后，至少通过以下检查：

1. 生命周期：初始化、运行、销毁路径完整且可重复进入。
2. 资源：事件订阅、池对象、文件句柄、网络连接均有释放路径。
3. 性能：热路径无明显新增分配，关键接口可在压测下稳定运行。
4. 调试：至少提供一个可观测入口（日志、统计、Editor 面板）。
5. 文档：根 README 补充该模块职责、接口、示例、注意事项。



