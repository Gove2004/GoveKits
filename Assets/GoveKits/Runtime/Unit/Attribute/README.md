# Runtime Unit Attribute Module

Attribute 模块用于描述并维护 Unit 数值体系。

## 设计理念

- 双层模型: StateAttribute 表示上限/基础，RuntimeAttribute 表示当前值。
- 规则透明: 通过 Modifier 明确加法、乘法、覆盖的结算含义。
- 变更可观测: 数值变动支持事件回调。

## 架构介绍

- UnitAttribute.cs
  - UnitAttribute 抽象基类
  - StateAttribute: 基础值 + Modifier 计算
  - RuntimeAttribute: 当前值 + 上限约束
- AttributeModifier.cs
  - ModifierType 与来源信息
- AttributeContainer.cs
  - 属性注册、查询、变更总入口

## 快速开始

1. 为 Unit 注册 StateAttribute（如 MaxHp、Atk）。
2. 为 Unit 注册 RuntimeAttribute（如 Hp、Mana）。
3. 通过 AttributeContainer.ApplyModifier 应用增益/减益。
4. 通过 ApplyChange 处理伤害/回复。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
