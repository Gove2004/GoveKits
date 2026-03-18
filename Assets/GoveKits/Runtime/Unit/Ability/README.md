# Runtime Unit Ability 开发文档

Ability 模块定义技能执行流程与规则约束。

## 设计理念

- 执行流程标准化。
- 规则与技能逻辑解耦。
- 容器统一调度。

## 架构介绍

- UnitAbility.cs: 技能基类（CanExecute / TryExecuteAsync / ExecuteAsync）
- AbilityRule.cs: 规则基类（Check / Commit）
- AbilityContainer.cs: 技能管理与执行

## 快速开始

```csharp
public sealed class FireballAbility : UnitAbility
{
    public override UnitTag Name => "Ability.Fireball";

    public FireballAbility(IUnit owner) : base(owner)
    {
    }

    public override async Cysharp.Threading.Tasks.UniTask ExecuteAsync(UnitContext context)
    {
        // 技能逻辑
        await Cysharp.Threading.Tasks.UniTask.CompletedTask;
    }
}
```

## 注意事项

- Check 只做判断，不要写副作用。
- Commit 内写副作用（如扣资源、加 CD）。
- AbilityContainer 中移除技能时会调用 Dispose。

## 相关跳转

- Unit: [../README.md](../README.md)
- Center: [../Center/README.md](../Center/README.md)
- Extension CD: [../Extension/README.md](../Extension/README.md)
