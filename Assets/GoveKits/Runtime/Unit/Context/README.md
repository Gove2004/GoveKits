# Runtime Unit Context Module

Context 模块定义 Unit 执行语义的公共上下文与查询能力。

## 设计理念

- 统一语义: 标签、查询、执行上下文保持一致表达。
- 可组合查询: TagQuery 支持逻辑组合匹配。
- 可扩展效果: UnitEffect 支持同步/异步执行模型。

## 架构介绍

- UnitTag.cs: 标签值对象
- TagQuery.cs: 标签逻辑组合表达式
- UnitContext.cs: 执行上下文（Source/Target/附加数据）
- UnitEffect.cs: 效果抽象与执行入口

## 快速开始

1. 用 UnitTag 定义统一标签常量。
2. 用 TagQuery 组织技能可用条件表达式。
3. 在 UnitContext 中传递执行上下文。
4. 通过 UnitEffect 封装命中后的效果逻辑。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Extension: [../Extension/README.md](../Extension/README.md)
