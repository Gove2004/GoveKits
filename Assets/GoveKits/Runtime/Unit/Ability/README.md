# Runtime Unit Ability Module

Ability 模块定义技能执行抽象、规则机制与容器管理。

## 设计理念

- 执行可控: 将技能执行与前置约束拆分。
- 规则可组合: 通过 AbilityRule 注入冷却、资源、状态限制。
- 容器统一: 统一注册、查询、触发、释放流程。

## 架构介绍

- UnitAbility.cs
  - 抽象技能类型
  - 执行入口与生命周期
- AbilityRule.cs
  - Check: 执行前检查
  - Commit: 执行时提交副作用
- AbilityContainer.cs
  - Add/Remove/Get
  - 按标签执行技能

## 快速开始

1. 继承 UnitAbility 定义技能行为。
2. 编写 AbilityRule（如冷却、消耗）并挂载到技能。
3. 通过 AbilityContainer 注册技能。
4. 在业务层按标签触发执行。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Center: [../Center/README.md](../Center/README.md)
- Extension: [../Extension/README.md](../Extension/README.md)
