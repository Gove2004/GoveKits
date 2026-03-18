# Runtime Core Pool 开发文档

Pool 提供 C# 对象与 GameObject 的复用能力。

## 设计理念

- 减少 GC 与 Instantiate/Destroy 抖动。
- 统一入口，统一回收语义。
- 运行时可视化监控缓存与活跃量。

## 架构介绍

- IPoolable.cs: 回收重置接口
- Pool.cs: 池实现
- PoolCore.cs: 对外 API

## 快速开始

```csharp
using GoveKits.Runtime.Core.Pool;

public class BulletData : IPoolable
{
    public float Speed;
    public void OnRecycle() => Speed = 0f;
}

PoolCore.Create<BulletData>(count: 8, maxSize: 64);
var data = PoolCore.Get<BulletData>();
data.Speed = 12f;
PoolCore.Return(data);
```

## 注意事项

- 池对象必须可重置，`OnRecycle` 不要留引用。
- GameObject 池 prefab 需要挂 `IPoolable` 组件。
- 超过 `maxSize` 的回收对象会被丢弃。

## 相关跳转

- Runtime Core: [../README.md](../README.md)
- Editor Pool Debugger: [../../../Editor/Core/README.md](../../../Editor/Core/README.md)
- Event: [../Event/README.md](../Event/README.md)
