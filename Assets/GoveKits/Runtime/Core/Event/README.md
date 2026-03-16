# GoveKits 事件系统

该目录提供了一个轻量、类型安全的事件系统，支持以下能力：

- 多总线路由（默认总线为 `main`）
- 监听器优先级
- 事件传播中断（`IsBreak`）
- 事件对象池化复用（与 `PoolCore` 集成）

## 文件说明

- `EventInfo.cs`：事件基类、监听器接口/基类、委托监听器、可释放反订阅句柄
- `EventBus.cs`：单总线内的订阅/取消订阅/发布实现（按事件类型分组）
- `EventCore.cs`：静态入口，提供总线管理和对外发布/订阅 API

## 核心概念

### 1) 事件类型

所有事件数据都必须继承 `EventInfo`，并实现 `OnRecycle()`。

```csharp
using GoveKits.Runtime.Core.Event;

public class DamageEvent : EventInfo
{
    public int Amount;
    public string Source;

    public override void OnRecycle()
    {
        Amount = 0;
        Source = null;
    }
}
```

`OnRecycle()` 的意义：
- 事件实例来自对象池，会被重复使用。
- 你必须重置所有可变字段，避免后续发布读到脏数据。

### 2) 发布流程

`EventCore.Publish<T>(Action<T> eventIniter, string busName = "main")`

运行流程：
1. `PoolCore.Get<T>()`
2. 通过 `eventIniter` 初始化事件内容
3. 在指定总线上分发给监听器
4. 在 `finally` 中归还对象到池

这样的设计可以降低 GC 分配，同时保持发布调用简洁。

### 3) 订阅方式

支持两种订阅形式：
- 强类型监听器类（`IEventListener<T>`）
- 委托回调（`Action<T>`，可带优先级）

两者都会返回 `DisposeAction`，不再需要时应及时 `Dispose()`。

## API 概览

### EventCore

- `GetOrCreateBus(string busName)`
- `DestroyBus(string busName)`
  - 销毁默认总线（`main`）会抛异常
- `Publish<T>(Action<T> eventIniter, string busName = "main") where T : EventInfo, new()`
- `Subscribe<T>(IEventListener<T> listener, string busName = "main") where T : EventInfo, new()`
- `Subscribe<T>(Action<T> callback, int priority = 0, string busName = "main") where T : EventInfo, new()`

### EventInfo

- `bool IsBreak`
  - 在某个监听器中设为 `true` 后，会中断当前事件后续监听器调用。
- `OnRecycle()`
  - 事件对象回收到对象池时调用。

### 监听器优先级

数值越大越先执行。

例如执行顺序：
- priority `100`
- priority `10`
- priority `0`
- priority `-10`

## 使用示例

### A) 使用回调订阅

```csharp
using GoveKits.Runtime.Core.Event;
using UnityEngine;

public class DamageLogger : MonoBehaviour
{
    private DisposeAction _subscription;

    private void OnEnable()
    {
        _subscription = EventCore.Subscribe<DamageEvent>(
            callback: e => Debug.Log($"Damage: {e.Amount}, Source: {e.Source}"),
            priority: 10
        );
    }

    private void OnDisable()
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
```

### B) 使用监听器类订阅

```csharp
using GoveKits.Runtime.Core.Event;
using UnityEngine;

public class BreakOnFatalDamageListener : EventListener<DamageEvent>
{
    public override int Priority => 100;

    public override void OnEvent(DamageEvent eventInfo)
    {
        if (eventInfo.Amount >= 999)
        {
            Debug.Log("Fatal damage detected. Stop propagation.");
            eventInfo.IsBreak = true;
        }
    }
}
```

### C) 发布事件

```csharp
using GoveKits.Runtime.Core.Event;

public static class DamageSender
{
    public static void SendDamage(int amount, string source)
    {
        EventCore.Publish<DamageEvent>(e =>
        {
            e.Amount = amount;
            e.Source = source;
        });
    }
}
```

### D) 使用自定义总线

```csharp
// 在自定义总线上订阅
var dispose = EventCore.Subscribe<DamageEvent>(e => { }, busName: "combat");

// 在同一自定义总线上发布
EventCore.Publish<DamageEvent>(e =>
{
    e.Amount = 5;
    e.Source = "SkillA";
}, busName: "combat");
```

## 最佳实践

- 事件数据尽量保持聚焦和轻量。
- 在 `OnRecycle()` 中重置所有可变字段。
- 始终释放订阅，尤其是在 Unity 生命周期函数中（`OnDisable`、`OnDestroy`）。
- 仅在确实有执行顺序要求时使用优先级。
- 若无明确边界需求，优先使用默认总线。

## 常见问题

- 忘记释放订阅，导致重复回调。
- 订阅在一个总线，发布在另一个总线。
- `OnRecycle()` 未重置字段，导致脏数据。
- 使用 `IsBreak` 时未考虑优先级执行顺序。

## 说明

- 该事件系统为同步分发：发布时会立即执行监听器。
- 若监听器抛异常，发布流程会包装后重新抛出（附带事件类型和总线信息）。
- 事件池化行为依赖 `PoolCore` 与 `EventInfo` 的重置实现。
