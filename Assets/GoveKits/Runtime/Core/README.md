# Runtime Core 开发文档

Core 是全项目公共底座。

## 设计理念

- 基础能力稳定、接口简洁。
- 与业务解耦，按需接入。
- 所有关键流程可观测。

## 架构介绍

- GoveKitsCore.cs: 日志
- Event/: 事件总线
- Pool/: 对象池
- Singleton/: 单例基类

## 快速开始

```csharp
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Core.Event;

GoveKitsCore.Log("Core", "startup");

EventCore.Publish<BootEvent>(e => { });
```

## 注意事项

- Core 不承载具体业务规则。
- 若出现循环依赖，优先通过 Event 解耦。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Event: [Event/README.md](Event/README.md)
- Pool: [Pool/README.md](Pool/README.md)
- Singleton: [Singleton/README.md](Singleton/README.md)
