# Runtime Core Event 开发文档

Event 提供同步、类型安全、可优先级排序的事件总线。

## 设计理念

- 事件作为模块间通信契约。
- 发布方与订阅方完全解耦。
- 支持调试可视化追踪。

## 架构介绍

- EventInfo.cs: 事件基类与监听器抽象
- EventBus.cs: 单总线路由与派发
- EventCore.cs: 全局入口

## 快速开始

```csharp
using GoveKits.Runtime.Core.Event;

public class DamageEvent : EventInfo
{
    public int Amount;
    public override void OnRecycle() => Amount = 0;
}

var dispose = EventCore.Subscribe<DamageEvent>(e =>
{
    UnityEngine.Debug.Log($"damage={e.Amount}");
}, priority: 10);

EventCore.Publish<DamageEvent>(e => e.Amount = 25);

dispose.Dispose();
```

## 注意事项

- `OnRecycle` 必须重置字段，避免脏数据。
- 跨总线使用时，订阅与发布的 `busName` 必须一致。
- `IsBreak` 会中断后续监听，注意优先级顺序。

## 相关跳转

- Runtime Core: [../README.md](../README.md)
- Editor Event Debugger: [../../../Editor/Core/README.md](../../../Editor/Core/README.md)
- Unit Reaction: [../../Unit/Reaction/README.md](../../Unit/Reaction/README.md)
