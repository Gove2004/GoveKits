# Res 模块文档

## 文档速览
- 目标: 统一多资源来源加载（Resources/AssetBundle/Addressables）并带引用计数缓存。
- 核心入口: `ResCore.Load`、`ResCore.LoadAsync`、`ResCore.Release`。
- 关键收益: 同一路径重复加载可命中缓存，减少重复 IO。

## 阅读路径
1. 先看 `ResCore.cs` 理解缓存与加载主流程。
2. 再看 `Loader/IResLoader.cs` 理解加载器统一接口。
3. 最后看各具体加载器实现与路径规则。

## 设计理念
- 统一入口: 调用方只依赖 `ResCore` 与 `ResLoadType`。
- 策略扩展: 不同资源系统由不同 Loader 承担。
- 引用计数: 命中缓存自动加引用，`Release` 归零时触发卸载。
- 类型安全: 泛型 API 限定目标资源类型。

## 架构介绍
- `ResCore`: 资源系统门面，组织加载器和缓存。
- `ResLoadType`: 标记加载来源。
- `IResLoader`: 加载器抽象（同步/异步/卸载）。
- `ResourcesResLoader`: 基于 `Resources`。
- `AssetBundleResLoader`: 基于 `AssetBundle`，支持 `bundle|asset` 路径格式。
- `AddressablesResLoader`: 基于 Addressables（需要宏支持）。

## 快速开始
### 1) 使用 Resources
```csharp
using GoveKits.Runtime.Storage.Res;
using UnityEngine;

GameObject prefab = ResCore.Load<GameObject>(ResLoadType.Resources, "Prefabs/Enemy");
```

### 2) 使用 AssetBundle
```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Res;
using UnityEngine;

public static async UniTask<GameObject> LoadEnemyAsync()
{
    // path 格式: bundlePath|assetPath
    return await ResCore.LoadAsync<GameObject>(
        ResLoadType.AssetBundle,
        "bundles/characters.ab|Enemy");
}
```

### 3) 释放资源引用
```csharp
using GoveKits.Runtime.Storage.Res;
using UnityEngine;

ResCore.Release<GameObject>(ResLoadType.Resources, "Prefabs/Enemy");
```

## 注意事项
- 缓存键包含加载类型、资源类型、路径，避免不同来源冲突。
- `Release<T>` 时 `T` 必须与加载时一致，否则键不同无法正确减引用。
- `AssetBundleResLoader` 的相对路径默认拼到 `Application.streamingAssetsPath`。
- Addressables 需要安装 `com.unity.addressables`（或 `com.unity.addressables.cn`）；`UNITASK_ADDRESSABLE_SUPPORT` 会通过 asmdef `versionDefines` 自动启用。
- `Resources.UnloadAsset` 不会处理 `GameObject/Component` 实例生命周期。

## 相关跳转
- `ResCore.cs`
- `Loader/IResLoader.cs`
- `Loader/ResourcesResLoader.cs`
- `Loader/AssetBundleResLoader.cs`
- `Loader/AddressablesResLoader.cs`
