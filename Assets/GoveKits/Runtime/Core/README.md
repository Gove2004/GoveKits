# Runtime Core 开发手册

Core 是 GoveKits 的基础能力层，负责提供跨模块通用能力，而不是承载具体业务逻辑。

## 设计理念

- API 要小而稳定: 对上层只暴露少量一致入口。
- 模块互相解耦: 通过事件与池化降低直接依赖。
- 运行期可排查: 关键机制可被 Editor 工具观察。

## 架构介绍

- GoveKitsCore.cs: 统一日志与基础工具
- Event/: 类型安全事件总线
- Pool/: 对象池与复用
- Singleton/: C# 与 MonoBehaviour 单例基类

建议使用顺序: 先接 Event 做解耦，再用 Pool 优化性能，最后按需引入 Singleton。

## 快速开始

### 1. 初始化日志与事件

```csharp
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Core.Event;

public sealed class BootEvent : EventInfo
{
    public string Message;
    public override void OnRecycle() => Message = null;
}

public static class CoreBoot
{
    public static void Run()
    {
        GoveKitsCore.Log("CoreBoot", "begin");
        EventCore.Publish<BootEvent>(e => e.Message = "boot ok");
    }
}
```

### 2. 把事件对象也纳入池化

```csharp
using GoveKits.Runtime.Core.Event;
using GoveKits.Runtime.Core.Pool;

public static class CoreOptimize
{
    public static void Warmup()
    {
        PoolCore.Create<BootEvent>(count: 8, maxSize: 64);
        EventCore.Publish<BootEvent>(e => e.Message = "pooled event");
    }
}
```

## 注意事项

- Core 只提供机制，不要在 Core 内写业务分支。
- Event 的 `OnRecycle` 必须完整重置字段。
- 池化对象要保证可重复使用，避免持有外部引用。

## 常见故障排查

- 现象: Core 日志没有输出。
    - 排查: 确认调用点是否命中，以及日志过滤级别是否屏蔽当前类型。
- 现象: 事件发布后没有订阅响应。
    - 排查: 检查订阅是否在发布之前注册、`busName` 是否一致。
- 现象: 池化后仍有明显 GC 峰值。
    - 排查: 确认高频对象是否真的走了 Pool，且 `OnRecycle` 是否重置干净。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Event: [Event/README.md](Event/README.md)
- Pool: [Pool/README.md](Pool/README.md)
- Singleton: [Singleton/README.md](Singleton/README.md)
- 术语与命名规范: [../../../../TERMINOLOGY.md](../../../../TERMINOLOGY.md)



