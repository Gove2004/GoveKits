# Runtime Unit Context 开发文档

Context 模块提供标签、查询、执行上下文与效果抽象。

## 设计理念

- 标签统一表达。
- 查询逻辑可组合。
- 效果执行模型可扩展。

## 架构介绍

- UnitTag.cs
- TagQuery.cs
- UnitContext.cs
- UnitEffect.cs

## 快速开始

```csharp
TagQuery canCast = !"State.Silenced" & !"State.Stunned";
if (canCast.Match(source.Attributes))
{
    var ctx = new UnitContext(source, target);
    await source.ApplyAsync(new DamageEffect(10));
}
```

## 注意事项

- Tag 命名请统一层级前缀。
- Context 推荐只承载执行态数据，不要塞全局单例。
- 异步 Effect 需保证异常后资源归还。

## 相关跳转

- Unit: [../README.md](../README.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Extension: [../Extension/README.md](../Extension/README.md)
