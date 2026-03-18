# Runtime AI FSM Module

FSM 模块提供轻量、可扩展、支持异步生命周期的有限状态机。

## 设计理念

- 状态职责清晰: 每个状态只处理自身行为与切换条件。
- 生命周期统一: OnEnter/OnUpdate/OnFixedUpdate/OnExit。
- 实战导向: 适用于角色行为、AI 控制、流程机驱动。

## 架构介绍

- IFSMObject.cs: 状态机宿主约束
- BaseState.cs: 状态基类与切换辅助
- FSM.cs: 状态注册、切换与更新驱动

## 快速开始

1. 定义状态枚举。
2. 宿主实现 IFSMObject 并注册状态。
3. 在 Update/FixedUpdate 中驱动 FSM。
4. 销毁时调用 Dispose。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Core: [../../Core/README.md](../../Core/README.md)
- Runtime Unit: [../../Unit/README.md](../../Unit/README.md)
