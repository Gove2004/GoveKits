# Runtime Core Singleton 开发手册

Singleton 模块提供两种单例基类: 纯 C# 服务对象与 Unity 组件对象。

## 设计理念

- 逻辑服务和场景组件分开建模。
- 初始化入口一致，减少重复模板代码。
- 单例只做“全局访问”，不承担复杂业务流程。

## 架构介绍

- CSharpSingleton<T>: 不依赖 Unity 生命周期
- MonoSingleton<T>: 依赖场景中的 GameObject

## 快速开始

### 1. C# 配置服务

```csharp
using GoveKits.Runtime.Core.Singleton;

public sealed class GameConfig : CSharpSingleton<GameConfig>
{
    public int MaxLevel;
    protected override void OnSingletonInit() => MaxLevel = 99;
}
```

### 2. Mono 行为服务

```csharp
using GoveKits.Runtime.Core.Singleton;
using UnityEngine;

public sealed class AudioHub : MonoSingleton<AudioHub>
{
    public void PlayClick() => Debug.Log("click");
}

AudioHub.Instance.PlayClick();
```

## 注意事项

- 非场景对象优先选 `CSharpSingleton<T>`。
- `MonoSingleton<T>` 多实例时不会自动删除冗余对象。
- 单例请保持轻量，避免成为“上帝对象”。

## 常见故障排查

- 现象: `Instance` 为 null。
    - 排查: `MonoSingleton` 是否在当前场景存在有效对象，生命周期是否被提前销毁。
- 现象: 出现多个单例对象。
    - 排查: 是否在多个场景或加载流程中重复放置同一单例组件。
- 现象: 单例状态在切场景后异常。
    - 排查: 区分应持久化的数据和场景态数据，避免混放在同一单例。

## 相关跳转

- Runtime Core: [../README.md](../README.md)
- Root: [../../../../../README.md](../../../../../README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



