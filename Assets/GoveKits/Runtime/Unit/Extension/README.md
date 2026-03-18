# Runtime Unit Extension 开发文档

Extension 模块承载 Unit 的扩展规则与 Unity 行为桥接。

## 设计理念

- 核心稳定，扩展独立。
- 战斗规则可插拔。
- Unity 生命周期与运行时容器对齐。

## 架构介绍

- CD.cs: CDRule + CDMark
- UnitBehaviour.cs: IUnit 的 MonoBehaviour 实现基类

## 快速开始

```csharp
var ability = new FireballAbility(owner);
ability.AddRule(new CDRule("CD.Fireball", 3f));
owner.Abilitys.AddAbility(ability);
```

## 注意事项

- CDTag 建议包含技能名，避免冲突。
- UnitBehaviour.OnDestroy 需清理容器。
- Editor 监控面板只在 Play 模式显示完整数据。

## 相关跳转

- Unit: [../README.md](../README.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Editor Unit: [../../../Editor/Unit/README.md](../../../Editor/Unit/README.md)
