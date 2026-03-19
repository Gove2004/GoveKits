# Runtime Core Pool 开发手册

Pool 模块负责对象复用，目标是降低分配频率和回收压力。

## 设计理念

- 临时对象尽量复用，不反复分配。
- 回收语义统一: 获取、使用、归还。
- 对象生命周期可观测，便于调试泄露。

## 架构介绍

- IPoolable: 对象回收重置协议
- Pool: 内部池结构
- PoolCore: 外部统一入口

## 快速开始

### 1. 纯 C# 对象池

```csharp
using GoveKits.Runtime.Core.Pool;

public sealed class BulletData : IPoolable
{
    public float Speed;
    public int Damage;
    public void OnRecycle()
    {
        Speed = 0f;
        Damage = 0;
    }
}

PoolCore.Create<BulletData>(count: 16, maxSize: 128);
var data = PoolCore.Get<BulletData>();
data.Speed = 18f;
PoolCore.Return(data);
```

### 2. 运行时批量预热

```csharp
using GoveKits.Runtime.Core.Pool;

public static class PoolWarmup
{
    public static void Init()
    {
        PoolCore.Create<BulletData>(count: 128, maxSize: 512);
    }
}
```

### 3. GameObject 对象池

```csharp
using GoveKits.Runtime.Core.Pool;
using UnityEngine;

public sealed class BulletView : MonoBehaviour, IPoolable
{
    public void OnRecycle()
    {
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}

public static class BulletSpawn
{
    public static void Spawn(GameObject bulletPrefab)
    {
        PoolCore.Create(bulletPrefab, count: 32, maxSize: 256);
        GameObject go = PoolCore.Get(bulletPrefab);

        // ... 使用对象

        PoolCore.Return(go);
    }
}
```

## 注意事项

- `OnRecycle` 要重置全部状态字段。
- 对象归还后禁止继续持有并修改。
- `maxSize` 过小会退化为频繁分配与回收。

## 常见故障排查

- 现象: GameObject 无法创建池。
    - 排查: prefab 上是否至少挂了一个实现 `IPoolable` 的组件。
- 现象: `PoolCore.Return(go)` 警告来源池丢失。
    - 排查: 该对象是否由 `PoolCore.Get(prefab)` 获取，而不是手动 Instantiate。
- 现象: 对象回池后状态残留。
    - 排查: 检查 `OnRecycle` 是否重置位置、动画、计时器等运行时状态。

## 相关跳转

- Runtime Core: [../README.md](../README.md)
- Editor Pool Debugger: [../../../Editor/Core/README.md](../../../Editor/Core/README.md)
- Event: [../Event/README.md](../Event/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



