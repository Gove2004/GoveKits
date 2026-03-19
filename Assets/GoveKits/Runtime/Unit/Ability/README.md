# Runtime Unit Ability 开发手册

Ability 负责“主动行为”的执行与约束，常用于技能、交互动作、战斗指令。

## 设计理念

- `CanExecute` 只判断，不改变状态。
- 规则负责约束，技能负责结果。
- 容器统一执行入口，避免散乱调用。

## 架构介绍

- UnitAbility: 技能抽象
- AbilityRule: 执行前后的规则钩子
- AbilityContainer: 注册、查找与执行

## 快速开始

### 1. 定义一个可执行技能

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Unit;

public sealed class FireballAbility : UnitAbility
{
    public override UnitTag Name => "Ability.Fireball";

    public FireballAbility(IUnit owner) : base(owner) { }

    public override async UniTask ExecuteAsync(UnitContext context)
    {
        context.Target.Attributes.Change("Attr.Hp", -20f);
        await UniTask.CompletedTask;
    }
}
```

### 2. 绑定规则并执行

```csharp
using GoveKits.Runtime.Unit;

public static class AbilityUseCase
{
    public static void Install(IUnit owner)
    {
        var ability = new FireballAbility(owner);
        ability.AddRule(new CDRule("CD.Fireball", 2f));
        owner.Abilities.AddAbility(ability);
    }
}
```

## 注意事项

- Rule 的副作用应放在 `Commit`，不是 `Check`。
- 能力标签必须全局稳定，避免热更后丢失映射。
- 容器移除能力会触发释放，避免二次持有引用。

## 常见故障排查

- 现象: `TryExecuteAsync` 返回 false。
    - 排查: 检查 `CanExecute` 与 Rule `Check` 的失败条件。
- 现象: 能力执行了但没产生结果。
    - 排查: 确认 `ExecuteAsync` 是否真正修改目标状态或触发 Effect。
- 现象: 冷却表现异常。
    - 排查: 检查 CD 标签是否唯一且与 Rule 中定义一致。

## 相关跳转

- Unit: [../README.md](../README.md)
- Center: [../Center/README.md](../Center/README.md)
- Extension CD: [../Extension/README.md](../Extension/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



