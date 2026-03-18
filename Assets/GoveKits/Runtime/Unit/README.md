# Runtime Unit Module

Unit 模块是战斗与角色数据建模的核心层，提供属性、标记、技能、反应、上下文与工厂中心。

## 设计理念

- 数据与行为分离: 属性/标记是状态，技能/反应是行为。
- 组合优先: 通过容器与规则组合能力，避免继承爆炸。
- 运行时友好: 与对象池、异步执行和调试面板协同。

## 架构介绍

模块分为 7 个子域:

- Context: 标签、查询、执行上下文、效果抽象
- Attribute: 状态值与运行时值体系
- Mark: 可叠层、可计时、可周期触发状态
- Ability: 技能执行流程与规则
- Reaction: 事件驱动被动系统
- Center: 注解扫描工厂中心
- Extension: 冷却规则与 MonoBehaviour 桥接

关键入口:

- IUnit.cs: Unit 统一接口
- READ.md: Unit 每个 cs 文件用途索引

## 快速开始

1. 定义 Unit 实现（推荐继承 UnitBehaviour）。
2. 在初始化阶段注册属性、技能、反应与默认标记。
3. 通过 AbilityContainer 执行技能，通过 ReactionContainer 处理被动。
4. 使用 UnitCenter 完成工厂扫描初始化。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Unit File Index: [READ.md](READ.md)
- Ability: [Ability/README.md](Ability/README.md)
- Attribute: [Attribute/README.md](Attribute/README.md)
- Center: [Center/README.md](Center/README.md)
- Context: [Context/README.md](Context/README.md)
- Extension: [Extension/README.md](Extension/README.md)
- Mark: [Mark/README.md](Mark/README.md)
- Reaction: [Reaction/README.md](Reaction/README.md)
