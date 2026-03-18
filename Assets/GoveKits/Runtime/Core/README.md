# Runtime Core Module

Core 模块提供全项目通用基础设施，是 Runtime 层的底座。

## 设计理念

- 稳定优先: 核心基础设施接口尽量稳定，减少业务层频繁改造。
- 通用抽象: 以最小抽象覆盖最多场景（Event、Pool、Singleton）。
- 低侵入: 不强耦合业务模型，可独立复用。

## 架构介绍

- GoveKitsCore.cs: 统一日志能力
- Event/: 类型安全事件总线
- Pool/: CSharp 与 GameObject 对象池
- Singleton/: CSharp 与 MonoBehaviour 单例基类

## 快速开始

1. 先接入 Event 发布订阅流程。
2. 在高频对象创建点接入 Pool。
3. 对全局服务类使用 Singleton 基类。
4. 用 GoveKitsCore 统一日志输出。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Event: [Event/README.md](Event/README.md)
- Pool: [Pool/README.md](Pool/README.md)
- Singleton: [Singleton/README.md](Singleton/README.md)
- Unit: [../Unit/README.md](../Unit/README.md)
