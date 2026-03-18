# GoveKits 2.0 开发文档

GoveKits 是一套面向 Unity 的运行时能力库与调试工具集合。

## 设计理念

- 业务可组合: 能力通过容器与规则组合，不依赖深层继承。
- 调试优先: Runtime 能力都配套 Editor 可视化入口。
- 文档即说明书: 文档强调“怎么接入、怎么排错、常见坑”。

## 架构介绍

- Runtime
  - Core: 日志、事件、对象池、单例
  - Unit: 属性、标记、技能、反应、上下文、工厂
  - AI/FSM: 状态机基础设施
- Editor
  - Core: Event/Pool 调试窗口
  - Unit: Unit 运行时监控 Inspector
- Plugins
  - 第三方依赖来源与接入说明

## 快速开始

1. 打开 [Runtime Core 文档](Assets/GoveKits/Runtime/Core/README.md)。
2. 根据 [Runtime Unit 文档](Assets/GoveKits/Runtime/Unit/README.md) 搭建一个最小 Unit。
3. 进入 Play 模式，使用 [Editor Core](Assets/GoveKits/Editor/Core/README.md) 调试窗口观察事件与池状态。

最小日志示例:

```csharp
using GoveKits.Runtime.Core;

GoveKitsCore.Log("Boot", "GoveKits initialized");
```

## 注意事项

- 事件订阅必须及时释放。
- 高频对象建议优先池化。
- Unit 模块创建实例推荐走 Center 工厂，避免散落 `new`。

## 相关跳转

- Runtime Core: [Assets/GoveKits/Runtime/Core/README.md](Assets/GoveKits/Runtime/Core/README.md)
- Runtime Unit: [Assets/GoveKits/Runtime/Unit/README.md](Assets/GoveKits/Runtime/Unit/README.md)
- Runtime AI/FSM: [Assets/GoveKits/Runtime/AI/FSM/README.md](Assets/GoveKits/Runtime/AI/FSM/README.md)
- Editor Core: [Assets/GoveKits/Editor/Core/README.md](Assets/GoveKits/Editor/Core/README.md)
- Editor Unit: [Assets/GoveKits/Editor/Unit/README.md](Assets/GoveKits/Editor/Unit/README.md)
- Plugins: [Assets/GoveKits/Plugins/README.md](Assets/GoveKits/Plugins/README.md)
