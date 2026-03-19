# Runtime Unit Context 开发手册

Context 模块定义执行上下文和标签表达，是 Ability/Effect 之间的共享协议层。

## 设计理念

- 用标签表达状态，而不是散落字符串判断。
- 用 Context 携带一次执行所需信息。
- Effect 作为最小行为单元，可复用可组合。

## 架构介绍

- UnitTag: 标签类型
- TagQuery: 条件组合表达式
- UnitContext: 执行态数据
- UnitEffect: 对目标施加变化

## 快速开始

### 1. 使用 TagQuery 做施法判定

```csharp
using GoveKits.Runtime.Unit;

TagQuery canCast = !"State.Silenced" & !"State.Stunned";
if (canCast.Match(source.Attributes))
{
    var ctx = new UnitContext(source, target);
    // continue execute...
}
```

### 2. 定义 Effect 并应用

```csharp
using GoveKits.Runtime.Unit;

public sealed class DamageEffect : UnitEffect<DamageEffect>
{
    public float Damage { get; private set; }

    public DamageEffect Set(float damage)
    {
        Damage = damage;
        return this;
    }

    public override void OnApply(IUnit target)
    {
        target.Attributes.Change("Attr.Hp", -Damage);
    }

    public override void OnRecycle() => Damage = 0f;
}

target.Apply(DamageEffect.Get().Set(10f));
```

## 注意事项

- Tag 前缀要统一，便于检索和调试。
- Context 只放执行时数据，不应承载全局状态。
- Effect 失败时要考虑补偿或幂等。

## 常见故障排查

- 现象: TagQuery 判断结果与预期相反。
    - 排查: 检查逻辑表达式中的 `!`、`&`、`|` 组合顺序。
- 现象: Effect 执行后没有改动目标。
    - 排查: 确认 `OnApply` 修改的是 `target`，不是 `source` 或局部变量。
- 现象: 执行链路上下文错乱。
    - 排查: 检查 `UnitContext(source, target)` 的实参传递是否正确。

## 相关跳转

- Unit: [../README.md](../README.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Extension: [../Extension/README.md](../Extension/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



