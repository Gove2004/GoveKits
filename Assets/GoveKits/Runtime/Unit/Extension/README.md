# Runtime Unit Extension Module

Extension 模块承载 Unit 的扩展能力与桥接组件。

## 设计理念

- 扩展隔离: 将可插拔机制与核心模型解耦。
- 规则复用: 通过 AbilityRule 扩展行为约束。
- Unity 桥接: 通过 UnitBehaviour 把运行时模型接到 MonoBehaviour。

## 架构介绍

- CD.cs
  - CDRule: 冷却检查与提交
  - CDMark: 冷却标记
- UnitBehaviour.cs
  - IUnit 的 Unity 行为实现基类
  - 生命周期内初始化与更新容器

## 快速开始

1. 在技能上添加 CDRule 并配置 CDTag 与时长。
2. 继承 UnitBehaviour 实现 InitAttributes/InitAbilitys 等初始化函数。
3. 在 Update 中由 UnitBehaviour 驱动 Mark 更新。
4. 用 Editor/Unit 监控面板观察运行状态。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
