# GoveKits 开发手册

GoveKits 是一个面向 Unity 项目的运行时基础库，重点解决三个问题: 模块解耦、战斗逻辑组织、运行期调试可观测。

## 设计理念

- 先约定再扩展: 统一入口和容器语义，避免各模块各写一套生命周期。
- 组合替代继承: Attribute、Mark、Ability、Reaction 通过容器协作。
- 调试并入开发流: Runtime 设计时就考虑 Editor 侧可视化。

## 架构介绍

### 模块分层

- Runtime/Core: 日志、事件总线、对象池、单例
- Runtime/Unit: 角色状态与行为系统
- Runtime/AI/FSM: 状态机基础能力
- Editor/Core 与 Editor/Unit: 运行期调试入口
- Plugins: 第三方能力来源与约束

### 典型执行链路

1. Unit 初始化容器与能力。
2. 业务通过 EventCore 或直接调用触发行为。
3. Ability 基于 Context 和规则执行，Mark/Attribute 发生变化。
4. Editor 面板观察运行时状态并定位问题。

## 快速开始

### 场景 1: 启动最小 Runtime

```csharp
using GoveKits.Runtime.Core;

public static class GameBoot
{
  public static void Init()
  {
    GoveKitsCore.Log("Boot", "GoveKits initialized");
  }
}
```

### 场景 2: 建立事件到技能的桥接

```csharp
using GoveKits.Runtime.Core.Event;
using GoveKits.Runtime.Unit;

public sealed class CastEvent : EventInfo
{
  public IUnit Caster;
  public UnitTag AbilityTag;
  public override void OnRecycle()
  {
    Caster = null;
    AbilityTag = default;
  }
}

public static class CastBridge
{
  public static DisposeAction Bind()
  {
    UnitCenter.Initialize();
    return EventCore.Subscribe<CastEvent>(e =>
    {
      var ctx = new UnitContext(e.Caster, e.Caster);
      e.Caster.Abilities.TryExecuteAsync(e.AbilityTag, ctx).Forget();
    });
  }
}
```

## 注意事项

- 事件监听必须可释放，避免跨场景遗留。
- 高频临时对象优先池化，减少 GC 峰值。
- Unit 子类型创建建议走 Center，保证注册与构造统一。

## 常见故障排查

- 现象: 示例代码可编译但运行无效果。
  - 排查: 先确认是否进入 Play 模式，再确认初始化入口是否实际执行。
- 现象: 技能调用没有触发。
  - 排查: 检查 `UnitCenter.Initialize()` 是否调用，以及 Ability 标签是否匹配。
- 现象: 调试面板和预期不一致。
  - 排查: 对照 Runtime 文档中的容器初始化顺序，确认没有跳过某个 Init 阶段。

## 相关跳转

- Runtime Core: [Assets/GoveKits/Runtime/Core/README.md](Assets/GoveKits/Runtime/Core/README.md)
- Runtime Unit: [Assets/GoveKits/Runtime/Unit/README.md](Assets/GoveKits/Runtime/Unit/README.md)
- Runtime AI/FSM: [Assets/GoveKits/Runtime/AI/FSM/README.md](Assets/GoveKits/Runtime/AI/FSM/README.md)
- Editor Core: [Assets/GoveKits/Editor/Core/README.md](Assets/GoveKits/Editor/Core/README.md)
- Editor Unit: [Assets/GoveKits/Editor/Unit/README.md](Assets/GoveKits/Editor/Unit/README.md)
- Plugins: [Assets/GoveKits/Plugins/README.md](Assets/GoveKits/Plugins/README.md)
- 术语与命名规范: [TERMINOLOGY.md](TERMINOLOGY.md)



