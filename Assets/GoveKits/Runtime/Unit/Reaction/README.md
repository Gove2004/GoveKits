# Runtime Unit Reaction Module

Reaction 模块用于实现事件驱动的被动技能系统。

## 设计理念

- 事件驱动: 被动能力通过 Event 触发，不主动轮询。
- 生命周期收敛: 由容器统一管理激活、停用、释放。
- 两种实现: 既支持继承，也支持委托快速接入。

## 架构介绍

- UnitReaction.cs
  - UnitReaction 抽象基类
  - UnitReaction<T>: 强类型事件反应
  - DelegateReaction<T>: 委托版反应
- ReactionContainer.cs
  - 增删查与批量启停

## 快速开始

1. 继承 UnitReaction<T> 或直接使用 DelegateReaction<T>。
2. 注册到 ReactionContainer。
3. 在合适时机 SetActive(true) 启动监听。
4. 对象销毁时 Clear 释放订阅。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Runtime Core Event: [../../Core/Event/README.md](../../Core/Event/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
