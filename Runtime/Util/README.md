# Util 模块

GoveKits 运行时基础子系统（v3.0.0 起由 Core 改名）。提供日志、对象池、事件总线、时间轮、实体生成等底层基础设施，以及 `More/` 下的通用工具类。所有 Core 均为静态类，通过 `GoveCore.Setup()` 统一初始化。

## 架构概览

```
GoveCore.Setup()
├── LogCore            → 多后端日志系统（Console / File / 自定义）
├── PoolCore           → CSharp 对象池 + GameObject 对象池
├── EventCore          → 类型安全的发布-订阅事件总线（池化事件，零 GC）
├── TimeCore           → 时间轮定时器（环形槽位 + 链表调度）
└── SpawnCore          → 实体注册/生成/销毁/查询

More/ 通用工具（namespace: GoveKits.Runtime.Util）
├── Singleton/         → CSharpSingleton / MonoSingleton
├── RNG/               → IRNG 接口 + NormalRNG
├── Math/              → Bezier 贝塞尔曲线
└── DisposeAction.cs   → IDisposable 委托封装

GoveCore.Close()     → 逆序释放所有资源
```

> 命名空间：`GoveKits.Runtime.Util`（v3.0.0 前为 `GoveKits.Runtime.Core`）。

## 初始化

```csharp
// 显式初始化所有 Core（推荐在 Start 中调用）
GoveCore.Setup();

// 关闭所有 Core（按逆序释放资源）
GoveCore.Close();
```

自定义配置示例：

```csharp
// 自定义日志配置：仅 Warning 及以上 + 文件输出
var fileLogger = new FileLogger("Logs/game.log");
LogCore.Setup(LogLevel.Warning, new List<ILogger> { fileLogger });

// 自定义时间轮参数（tick 间隔 100ms，1024 个槽位）
TimeCore.Setup(tickDuration: 0.1f, wheelSize: 1024);
```

## LogCore — 日志系统

支持日志等级过滤、多后端并行输出和自定义 Logger 扩展。

```csharp
LogCore.Setup();
LogCore.Info("Game", "游戏启动");
LogCore.Error("Game", $"出错了: {ex.Message}");

// 添加额外后端
LogCore.AddLogger(new UnityLogger());
```

实现 `ILogger` 接口即可添加自定义后端：

```csharp
public class MyLogger : ILogger
{
    public void Log(LogLevel level, string tag, string message, string colorHex = null)
    {
        // 发送到远程服务器、数据库等
    }
    public void Close() { }
}
```

## PoolCore — 对象池系统

管理两类对象池：CSharp 泛型对象池和 Unity GameObject 对象池。所有池化对象需实现 `IPoolable` 接口。

```csharp
// CSharp 对象池（预热 8 个，最大 64 个）
var pool = PoolCore.Create<MyClass>(count: 8, maxSize: 64);
var obj = PoolCore.Get<MyClass>();
PoolCore.Return(obj);

// GameObject 对象池
var pool = PoolCore.Create(prefab, count: 8, maxSize: 64);
var go = PoolCore.Get(prefab);
PoolCore.Return(go);  // 禁用而非销毁
```

```csharp
public class MyClass : IPoolable
{
    public int Value { get; set; }
    public void OnRecycle() { Value = 0; }  // 归还时重置状态
}
```

## EventCore — 事件系统

类型安全的发布-订阅事件总线。事件对象从 PoolCore 获取，发布后自动归还到池，零 GC 分配。

```csharp
// 定义事件（继承 EventData，实现 OnRecycle）
public class PlayerMoveEvent : EventData
{
    public Vector3 Position;
    public override void OnRecycle() { Position = default; }
}

// 订阅（返回 IDisposable 用于取消订阅）
var sub = EventCore.Subscribe<PlayerMoveEvent>(new MyListener());

// 发布（自动归还到池）
var evt = EventCore.GetEvent<PlayerMoveEvent>();
evt.Position = player.transform.position;
EventCore.Publish(evt);

sub.Dispose();
```

## TimeCore — 时间轮定时器

基于环形槽位的时间轮算法。一个 TimeCore 实例对应一个独立的时间轮，由 `TimeWheel` 内部管理。

```csharp
// 一次性定时器（1 秒后执行）
var timer = TimeCore.Once(1.0f, () => Debug.Log("1秒后执行"));

// 循环定时器（每 0.5 秒执行，共 10 次）
var timer = TimeCore.Loop(0.5f, () => Debug.Log("每0.5秒"), loopCount: 10);

timer.Pause(); timer.Resume(); timer.Cancel();

// 驱动（Update 中调用）
TimeCore.Tick(Time.deltaTime);
```

## SpawnCore — 实体生成系统

管理工厂注册、实体生命周期和 ID 分配。所有生成的实体必须实现 `ISpawnable` 接口。

```csharp
SpawnCore.Register("enemy",
    (objectId, data) => new EnemyEntity { ObjectId = objectId },
    entity => { /* 清理资源 */ });

var entity = SpawnCore.Spawn("enemy");
SpawnCore.Despawn(entity.ObjectId);

SpawnCore.OnEntitySpawned += entity => Debug.Log($"生成: {entity.ObjectId}");
SpawnCore.OnEntityDespawned += entity => Debug.Log($"销毁: {entity.ObjectId}");
```

## More/ 通用工具

| 工具 | 说明 |
|------|------|
| `MonoSingleton<T>` / `CSharpSingleton<T>` | 单例基类（Mono / 纯 C#） |
| `IRNG` / `NormalRNG` | 随机数接口与实现，纯 C# 不依赖 Unity，支持种子重置与正态分布 |
| `Bezier` | 贝塞尔曲线计算 |
| `DisposeAction` | 把委托包装为 `IDisposable` |

## 注意事项

- **所有 Core 均为静态类**，不能继承或实例化。通过 `Setup()` 方法配置默认行为。
- **单线程模型**：除异步 API 外，所有 Core 均假设在主线程调用。
- **关闭顺序**：`GoveCore.Close()` 按严格逆序释放所有资源，无需手动调用各 Core 的 `Close()`。
- **HTTP 与网络**见 [Runtime/Network/README.md](../Network/README.md)（UnityWebRequest 封装 + 内置 Mirror）。
