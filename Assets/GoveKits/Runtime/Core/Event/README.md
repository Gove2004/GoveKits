# Runtime Core Event 开发手册

Event 模块用于模块间通信。它强调“约定事件类型”而不是“直接互调对象”。

## 设计理念

- 发布方不知道订阅方，订阅方也不关心发布来源。
- 使用类型定义契约，降低字符串事件名带来的隐式错误。
- 优先级与中断语义可控，便于流程编排。

## 架构介绍

- EventInfo: 所有事件数据基类，负责回收重置
- EventBus: 单总线监听与派发
- EventCore: 全局总线入口

## 快速开始

### 1. 定义并发布事件

```csharp
using GoveKits.Runtime.Core.Event;

public sealed class DamageEvent : EventInfo
{
    public int Value;
    public override void OnRecycle() => Value = 0;
}

public static class DamageEmitter
{
    public static void Emit(int value)
    {
        EventCore.Publish<DamageEvent>(e => e.Value = value);
    }
}
```

### 2. 订阅、分优先级、释放

```csharp
using GoveKits.Runtime.Core.Event;

public static class DamageListeners
{
    public static void Bind()
    {
        var guard = EventCore.Subscribe<DamageEvent>(e =>
        {
            if (e.Value > 999) e.IsBreak = true;
        }, priority: 100, busName: "combat");

        var log = EventCore.Subscribe<DamageEvent>(e =>
        {
            UnityEngine.Debug.Log($"damage={e.Value}");
        }, priority: 0, busName: "combat");

        guard.Dispose();
        log.Dispose();
    }
}
```

## 注意事项

- 同一业务域的 `busName` 要保持一致。
- 忘记 `Dispose` 是最常见的内存与逻辑泄露来源。
- `IsBreak` 会终止后续监听，必须只在必要时使用。

## 常见故障排查

- 现象: 订阅回调完全不触发。
    - 排查: 检查事件类型是否一致、订阅是否提前释放。
- 现象: 只有部分监听生效。
    - 排查: 检查高优先级监听中是否设置了 `IsBreak = true`。
- 现象: 数据串台或脏数据。
    - 排查: 检查 `EventInfo.OnRecycle` 是否完整重置字段。

## 相关跳转

- Runtime Core: [../README.md](../README.md)
- Editor Event Debugger: [../../../Editor/Core/README.md](../../../Editor/Core/README.md)
- Unit Reaction: [../../Unit/Reaction/README.md](../../Unit/Reaction/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



