# Runtime Core Singleton Module

Singleton 模块提供 CSharp 与 MonoBehaviour 两套单例基类。

## 设计理念

- 区分运行上下文: 纯逻辑与 Unity 组件分开处理。
- 降低重复代码: 统一初始化与实例获取逻辑。
- 明确边界: 单例用于基础服务，不承载复杂业务流程。

## 架构介绍

- CSharpSingleton.cs: 线程安全、延迟初始化单例
- MonoSingleton.cs: Unity 组件单例，支持自动查找/创建

## 快速开始

1. 纯服务类继承 CSharpSingleton<T>。
2. 组件管理类继承 MonoSingleton<T>。
3. 在 OnSingletonInit 中完成初始化。
4. 通过 Instance 统一访问。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Core: [../README.md](../README.md)
- Event: [../Event/README.md](../Event/README.md)
- Unit: [../../Unit/README.md](../../Unit/README.md)
