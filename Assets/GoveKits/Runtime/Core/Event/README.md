# Runtime Core Event Module

Event 模块提供同步、类型安全、支持优先级与总线隔离的事件系统。

## 设计理念

- 事件即契约: 通过 EventInfo 明确消息结构。
- 发布订阅解耦: 发送方不依赖接收方实现。
- 可观测性: 配套调试窗口查看频道与历史。

## 架构介绍

- EventInfo.cs: 事件基类与监听器抽象
- EventBus.cs: 单总线内部路由与派发
- EventCore.cs: 全局入口与总线管理

核心能力:

- 多总线（默认 main）
- 监听优先级
- IsBreak 中断后续监听
- 与 PoolCore 集成的事件对象复用

## 快速开始

1. 定义事件类型（继承 EventInfo）。
2. Subscribe 订阅事件。
3. Publish 发布事件并初始化数据。
4. 在对象生命周期末尾 Dispose 订阅。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Core: [../README.md](../README.md)
- Editor Core: [../../../Editor/Core/README.md](../../../Editor/Core/README.md)
- Unit: [../../Unit/README.md](../../Unit/README.md)
