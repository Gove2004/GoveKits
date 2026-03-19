# Runtime Unit Reaction 开发手册

Reaction 负责事件驱动的被动逻辑，常用于受击触发、连锁效果、自动响应。

## 设计理念

- 事件触发优于轮询判断。
- 被动能力也是可管理对象，必须可启停、可清理。
- 先用委托快速实现，再按需升级到自定义类。

## 架构介绍

- UnitReaction: 被动逻辑基类
- DelegateReaction<TEvent>: 委托式快速实现
- ReactionContainer: 被动注册与激活管理

## 快速开始

### 1. 绑定受击触发

```csharp
using GoveKits.Runtime.Core.Event;
using GoveKits.Runtime.Unit;

public sealed class DamageEvent : EventInfo
{
    public IUnit Target;
    public float Value;
    public override void OnRecycle() { Target = null; Value = 0f; }
}

var reaction = new DelegateReaction<DamageEvent>(owner, "Reaction.Counter", e =>
{
    if (ReferenceEquals(e.Target, owner))
    {
        owner.Attributes.Change("Attr.Hp", 1f);
    }
});
```

### 2. 加入容器并激活

```csharp
owner.Reactions.AddReaction(reaction);
owner.Reactions.SetActive(true);
```

## 注意事项

- 添加后默认不激活，需要显式启用。
- 同名 Reaction 通常会覆盖旧实例，命名要稳定。
- 销毁 Unit 时清理容器，避免事件订阅残留。

## 常见故障排查

- 现象: 被动逻辑从未触发。
    - 排查: 检查 Reaction 是否加入容器且 `SetActive(true)`。
- 现象: 同一事件触发次数异常。
    - 排查: 检查是否重复注册同名 Reaction 或重复订阅。
- 现象: Unit 销毁后仍收到事件。
    - 排查: 检查容器清理与订阅释放是否在销毁阶段执行。

## 相关跳转

- Unit: [../README.md](../README.md)
- Runtime Core Event: [../../Core/Event/README.md](../../Core/Event/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



