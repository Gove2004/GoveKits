# Pools

对象池系统：支持 C# 对象与 Unity 游戏对象的高效复用，基于栈和 Unity ObjectPool 实现。

## 架构
- `Pool`：统一入口（外观类），自动选择 C# 池或 Unity 池。
- `CSharpPool<T>`：内部实现，为 C# 类提供栈式对象池。
- `UnityPool`：内部实现，为 GameObject 维护动态池字典（按 Prefab）。
- `PoolRecord`：标记组件，记录 GameObject 所属的池（便于回收）。
- `IPoolable`：生命周期回调接口（OnRecycle）。
- `PoolConfig`：池常量配置（容量、尺寸、集合检查等）。

## 使用示例
```csharp
// C# 对象
var evt = Pool.Get<MyEvent>();  // 从池取或新建
// ... 使用 evt
evt.OnRecycle();  // 清理
Pool.Recycle(evt);  // 回归池中

// Unity 组件（推荐方式）
var bullet = Pool.Get(bulletPrefab);  // 泛型方式，类型安全
// ... 使用 bullet
Pool.Recycle(bullet);

// GameObject
var go = Pool.Get(myPrefab);
Pool.Recycle(go);
```

详见源码中的 XML 注释。