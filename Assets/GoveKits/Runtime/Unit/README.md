# Runtime Unit 开发手册

Unit 模块用于组织角色系统，核心目标是让状态、行为、规则、事件解耦并可组合。

## 设计理念

- 状态和行为分离: Attribute/Mark 存状态，Ability/Reaction 执行业务。
- 容器协作: 所有子系统通过容器管理生命周期。
- 约定优先: 标签命名、初始化顺序、执行上下文有统一约束。

## 架构介绍

### 子模块职责

- Ability: 主动技能及规则管线
- Attribute: 数值、上限、修饰器
- Center: 注解注册工厂
- Context: 标签表达与执行上下文
- Extension: CD 与 UnitBehaviour 桥接
- Mark: 持续状态、层数、周期效果
- Reaction: 事件驱动的被动逻辑

### 标准初始化顺序

1. InitAttributes
2. InitMarks
3. InitAbilities
4. InitReactions

## 快速开始

### 1. 创建最小 Unit

```csharp
using GoveKits.Runtime.Unit;

public sealed class HeroUnit : UnitBehaviour
{
    public override void InitAttributes()
    {
        var maxHp = Attributes.AddState("Attr.MaxHp", 120f);
        Attributes.AddRuntime("Attr.Hp", maxHp);
    }

    public override void InitMarks() { }
    public override void InitAbilities() { }
    public override void InitReactions() { }
}
```

### 2. 触发技能执行

```csharp
using GoveKits.Runtime.Unit;

public static class UnitCast
{
    public static void CastSelf(IUnit unit, UnitTag abilityTag)
    {
        var ctx = new UnitContext(unit, unit);
        unit.Abilities.TryExecuteAsync(abilityTag, ctx).Forget();
    }
}
```

### 3. 应用即时效果

```csharp
using GoveKits.Runtime.Unit;

public static class UnitEffectUseCase
{
    public static void Damage(IUnit target, float value)
    {
        var effect = AttributeChangeEffect.Get().Set("Attr.Hp", -value);
        target.Apply(effect);
    }
}
```

## 注意事项

- 标签建议分层: `Ability.Xxx`、`Mark.Xxx`、`Attr.Xxx`。
- 执行异常优先检查 `CanExecute` 与 Rule 的 `Check/Commit`。
- 清理阶段需要释放 Reaction 订阅与容器内容。

## 常见故障排查

- 现象: Unit 运行中某个容器一直为空。
    - 排查: 对应 `Init*` 方法是否实现并被调用。
- 现象: 技能或状态标签匹配不上。
    - 排查: 检查标签是否遵循 `Ability.* / Mark.* / Attr.*` 约定。
- 现象: 回合后数据不重置。
    - 排查: 检查容器清理流程和对象回池逻辑是否完整执行。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Ability: [Ability/README.md](Ability/README.md)
- Attribute: [Attribute/README.md](Attribute/README.md)
- Center: [Center/README.md](Center/README.md)
- Context: [Context/README.md](Context/README.md)
- Extension: [Extension/README.md](Extension/README.md)
- Mark: [Mark/README.md](Mark/README.md)
- Reaction: [Reaction/README.md](Reaction/README.md)
- 术语与命名规范: [../../../../TERMINOLOGY.md](../../../../TERMINOLOGY.md)



