# Editor Unit Module

Editor/Unit 提供 Unit 系统运行时 Inspector 监控能力。

## 设计理念

- 面向战斗调试: 直接在 Inspector 查看关键战斗状态。
- 数据直观: 属性、标记、技能、反应分组可视化。
- 低成本接入: 继承 UnitBehaviour 即可复用监控面板。

## 架构介绍

- UnitBehaviourEditor.cs
  - 自定义 Inspector
  - 实时刷新
  - 过滤与折叠面板
  - 冷却状态可视化

## 快速开始

1. 让角色类继承 UnitBehaviour。
2. 进入 Play 模式并选中 Unit 对象。
3. 在 Inspector 中查看 Attributes/Marks/Abilities/Reactions。
4. 用过滤器快速定位目标标签。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Runtime Unit: [../../Runtime/Unit/README.md](../../Runtime/Unit/README.md)
- Unit File Index: [../../Runtime/Unit/READ.md](../../Runtime/Unit/READ.md)
