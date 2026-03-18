# Runtime Core Singleton 开发文档

Singleton 提供纯 C# 与 MonoBehaviour 两种单例模式。

## 设计理念

- 明确场景: 逻辑服务与组件服务分开。
- 降低模板代码与初始化重复。
- 保持单例职责单一。

## 架构介绍

- CSharpSingleton.cs
- MonoSingleton.cs

## 快速开始

```csharp
using GoveKits.Runtime.Core.Singleton;

public class GameConfig : CSharpSingleton<GameConfig>
{
    public int MaxLevel;
    protected override void OnSingletonInit() => MaxLevel = 100;
}

public class AudioService : MonoSingleton<AudioService>
{
}
```

## 注意事项

- 非 Unity 生命周期对象优先 CSharpSingleton。
- MonoSingleton 多实例只告警，不会自动清理多余实例。

## 相关跳转

- Runtime Core: [../README.md](../README.md)
- Root: [../../../../../README.md](../../../../../README.md)
