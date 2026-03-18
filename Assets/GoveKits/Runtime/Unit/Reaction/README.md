# Runtime Unit Reaction 开发文档

Reaction 模块用于实现事件驱动被动技能。

## 设计理念

- 事件触发，不轮询。
- 容器管理生命周期。
- 支持委托快速接入与继承深定制。

## 架构介绍

- UnitReaction.cs
- DelegateReaction<T>
- ReactionContainer.cs

## 快速开始

```csharp
var reaction = new DelegateReaction<DamageEvent>(
    owner,
    "Reaction.Counter",
    e =>
    {
        if (e.Target == owner)
        {
            // 被动触发
        }
    },
    priority: 10);

owner.Reactions.AddReaction(reaction);
owner.Reactions.SetActive(true);
```

## 注意事项

- Add 后默认不激活，需显式 SetActive(true)。
- 同名 Reaction 会替换旧实例。
- 销毁时调用 Clear，避免订阅泄露。

## 相关跳转

- Unit: [../README.md](../README.md)
- Runtime Core Event: [../../Core/Event/README.md](../../Core/Event/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
