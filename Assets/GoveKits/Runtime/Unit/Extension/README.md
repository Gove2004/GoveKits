# Runtime Unit Extension 开发手册

Extension 提供 Unit 与 Unity 生命周期之间的桥接能力，并承载常用规则扩展（如 CD）。

## 设计理念

- Runtime 核心保持纯逻辑，Unity 依赖集中在 Extension。
- 规则可插拔，不侵入每个 Ability 实现。
- 生命周期明确，便于销毁和重建。

## 架构介绍

- UnitBehaviour: IUnit 的 MonoBehaviour 实现
- CDRule/CDMark: 冷却约束与状态表示

## 快速开始

### 1. 在 UnitBehaviour 中安装能力

```csharp
using GoveKits.Runtime.Unit;

public sealed class SkillUnit : UnitBehaviour
{
    public override void InitAttributes() { }
    public override void InitMarks() { }

    public override void InitAbilities()
    {
        var ability = new FireballAbility(this);
        ability.AddRule(new CDRule("CD.Fireball", 2f));
        Abilities.AddAbility(ability);
    }

    public override void InitReactions() { }
}
```

### 2. 通过标签触发技能

```csharp
using GoveKits.Runtime.Unit;

public static class ExtensionUseCase
{
    public static void Cast(IUnit unit)
    {
        var ctx = new UnitContext(unit, unit);
        unit.Abilities.TryExecuteAsync("Ability.Fireball", ctx).Forget();
    }
}
```

## 注意事项

- CD 标签需要稳定命名，建议 `CD.AbilityName`。
- Mono 生命周期结束时应清理容器与订阅。
- 编辑器监控只代表运行期快照，不是持久状态。

## 常见故障排查

- 现象: Ability 添加成功但执行无响应。
    - 排查: 检查是否在 `InitAbilities` 里完成注册并调用了执行入口。
- 现象: CD 一直不结束或反复触发。
    - 排查: 检查 CD 的 Tag 是否冲突、Mark 更新是否正常推进。
- 现象: 场景卸载后仍有行为残留。
    - 排查: 检查 `OnDestroy` 阶段是否清理容器与事件订阅。

## 相关跳转

- Unit: [../README.md](../README.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Editor Unit: [../../../Editor/Unit/README.md](../../../Editor/Unit/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



