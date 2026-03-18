# Runtime Unit Center Module

Center 模块用于集中管理 Unit 子系统工厂。

## 设计理念

- 统一创建: 通过中心工厂减少散落 new。
- 注解驱动: 仅扫描显式标注类型，避免误注册。
- 入口收敛: 统一由 UnitCenter 初始化。

## 架构介绍

- FactoryScanAttribute.cs
  - FactoryAutoRegisterAttribute: 自动注册标记
- AbilityCenter.cs / MarkCenter.cs / ReactionCenter.cs
  - 扫描并缓存工厂
  - Create(Type) / Create<T>
- UnitCenter.cs
  - 统一初始化入口

## 快速开始

1. 在 Ability/Mark/Reaction 实现类上加 FactoryAutoRegisterAttribute。
2. 游戏启动时调用 UnitCenter.Initialize()。
3. 在业务中用对应 Center 创建实例。
4. 类型未注册时优先检查注解与构造签名。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
