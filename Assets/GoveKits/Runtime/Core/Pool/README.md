# Runtime Core Pool Module

Pool 模块提供对象复用体系，包含纯 CSharp 对象池与 GameObject 对象池。

## 设计理念

- 降低 GC 压力: 减少高频 new/Destroy。
- 统一入口: 通过 PoolCore 管理两类池。
- 生命周期显式: 回收时统一重置对象状态。

## 架构介绍

- IPoolable.cs: 池对象重置接口
- Pool.cs: 具体池实现
- PoolCore.cs: 对外统一创建、获取、归还、调试接口

## 快速开始

1. 让可池化对象实现 IPoolable.OnRecycle。
2. 用 PoolCore.Create 创建池。
3. 业务中通过 Get/Return 复用对象。
4. 打开 Pool Debugger 观察缓存与活跃量。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Core: [../README.md](../README.md)
- Editor Pool Debugger: [../../../Editor/Core/README.md](../../../Editor/Core/README.md)
- Unit: [../../Unit/README.md](../../Unit/README.md)
