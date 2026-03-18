# Runtime Unit 开发文档

Unit 是战斗与角色逻辑的核心模块，覆盖数值、状态、技能、被动与执行上下文。

## 设计理念

- 状态与行为分离: Attribute/Mark 保存状态，Ability/Reaction 承载行为。
- 组合优先: 容器 + 规则组合，减少硬编码分支。
- 调试友好: 结构与 Editor Unit 面板一一对应。

## 架构介绍

子模块:

- Ability: 技能与规则
- Attribute: 数值体系
- Center: 工厂集中初始化
- Context: 标签/上下文/效果
- Extension: 冷却与 MonoBehaviour 桥接
- Mark: 状态系统
- Reaction: 事件被动系统

主入口:

- IUnit.cs
- UnitBehaviour.cs（Extension）

## 快速开始

```csharp
public class HeroUnit : UnitBehaviour
{
    public override void InitAttributes()
    {
        var maxHp = Attributes.AddState("MaxHp", 100);
        Attributes.AddRuntime("Hp", maxHp);
    }

    public override void InitMarks() { }
    public override void InitAbilitys() { }
    public override void InitReactions() { }
}
```

## 注意事项

- 初始化顺序建议: Attributes -> Marks -> Abilities -> Reactions。
- 标签命名建议统一前缀（如 `CD.Skill.Fireball`）。
- 执行链路问题优先看 Context 与 AbilityRule。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Ability: [Ability/README.md](Ability/README.md)
- Attribute: [Attribute/README.md](Attribute/README.md)
- Center: [Center/README.md](Center/README.md)
- Context: [Context/README.md](Context/README.md)
- Extension: [Extension/README.md](Extension/README.md)
- Mark: [Mark/README.md](Mark/README.md)
- Reaction: [Reaction/README.md](Reaction/README.md)
