# Core 模块

GoveKits 运行时核心子系统。提供日志、对象池、随机数、事件总线、时间轮、实体生成等底层基础设施。所有 Core 均为静态类，通过 `GoveCore.Setup()` 统一初始化。

## 架构概览

```
GoveCore.Setup()
├── LogCore            → 多后端日志系统（Console / File / 自定义）
├── PoolCore           → CSharp 对象池 + GameObject 对象池
├── RandomCore         → 随机数系统（IRNG 接口 + NormalRNG）
├── EventCore          → 类型安全的发布-订阅事件总线
├── TimeCore           → 时间轮定时器（环形槽位 + 链表调度）
├── SpawnCore          → 实体注册/生成/销毁/查询
└── HttpCore           → HTTP 请求封装（Fluent API）

GoveCore.Close()     → 逆序释放所有资源
```

## 初始化

```csharp
// 首次访问任意静态 Core 时可直接使用（懒加载）
LogCore.Info("Game", "游戏启动");
var obj = PoolCore.Get<MyClass>();

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

// 自定义随机数种子
RandomCore.Setup(new NormalRNG(42));

// 自定义时间轮参数（tick 间隔 100ms，1024 个槽位）
TimeCore.Setup(tickDuration: 0.1f, wheelSize: 1024);
```

注意：所有 Core 都是静态类，不能继承或自定义装配。如需修改默认行为，直接调用对应的 `Setup()` 方法即可。

## LogCore — 日志系统

支持日志等级过滤、多后端并行输出和自定义 Logger 扩展。

### 架构

```
LogCore
├── LogLevel 过滤（Verbose / Debug / Info / Warning / Error）
├── OnLog 事件（监听所有日志输出）
└── List<ILogger> 后端
    ├── UnityLogger  → 输出到 UnityEngine.Debug
    └── FileLogger   → 写入日志文件
```

### 用法

```csharp
// 默认：Debug 等级 + 空日志后端列表
LogCore.Setup();
LogCore.Info("Game", "游戏启动");
LogCore.Error("Game", $"出错了: {ex.Message}");

// 自定义：仅 Warning 及以上 + 文件输出
var fileLogger = new FileLogger("Logs/game.log");
LogCore.Setup(LogLevel.Warning, new List<ILogger> { fileLogger });

// 添加额外后端
LogCore.AddLogger(new UnityLogger());
```

### 扩展

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

### CSharp 对象池

```csharp
// 创建指定类型的池（预热 8 个，最大 64 个）
var pool = PoolCore.Create<MyClass>(count: 8, maxSize: 64);

// 获取 / 归还
var obj = PoolCore.Get<MyClass>();
PoolCore.Return(obj);

// 清空指定类型的池
PoolCore.Clear<MyClass>();
```

池化对象示例：

```csharp
public class MyClass : IPoolable
{
    public int Value { get; set; }

    public void OnRecycle()
    {
        Value = 0;  // 归还时重置状态
    }
}
```

### GameObject 对象池

```csharp
var pool = PoolCore.Create(prefab, count: 8, maxSize: 64);
var go = PoolCore.Get(prefab);
PoolCore.Return(go);  // 禁用而非销毁
PoolCore.Clear(prefab);
```

池化的 GameObject 会自动挂载 `PoolRecord` 组件。`Return` 时递归调用子物体上所有 `IPoolable.OnRecycle()`。

## RandomCore — 随机数系统

基于 `IRNG` 接口的随机数系统，默认使用 `NormalRNG`（封装 `System.Random`）。

### 用法

```csharp
RandomCore.Setup();

RandomCore.NextInt(100);         // [0, 100) 随机整数
RandomCore.NextFloat();          // [0.0, 1.0) 随机浮点数
RandomCore.Chance(0.3f);         // 30% 概率返回 true
RandomCore.Pick(myList);         // 从列表中随机选取一项
RandomCore.Shuffle(myList);      // 原地打乱列表
RandomCore.NextGaussian(50, 10); // 正态分布随机数（均值 50，标准差 10）

// 自定义种子（用于可重复测试）
RandomCore.Setup(new NormalRNG(42));
```

### IRNG 接口

```csharp
public interface IRNG
{
    int Seed { get; }
    void Reseed(int seed);
    int NextInt();
    int NextInt(int maxExclusive);
    int Range(int minInclusive, int maxExclusive);
    float NextFloat();
    float Range(float minInclusive, float maxInclusive);
    bool Chance(float probability);
    bool NextBool();
    int NextSign();
    float NextGaussian(float mean = 0f, float stdDev = 1f);
    T Pick<T>(IReadOnlyList<T> list);
    List<T> PickMultiple<T>(IEnumerable<T> source, int count);
    T PickWeighted<T>(IEnumerable<T> items, Func<T, float> weightSelector);
    void Shuffle<T>(IList<T> list);
}
```

## EventCore — 事件系统

类型安全的发布-订阅事件总线。事件对象从 PoolCore 获取，发布后自动归还到池，零 GC 分配。

### 用法

```csharp
// 定义事件（继承 EventData，实现 OnRecycle）
public class PlayerMoveEvent : EventData
{
    public Vector3 Position;
    public override void OnRecycle() { Position = default; }
}

// 订阅（返回 IDisposable 用于取消订阅）
var sub = EventCore.Subscribe<PlayerMoveEvent>(new MyListener());

// 发布
var evt = EventCore.GetEvent<PlayerMoveEvent>();
evt.Position = player.transform.position;
EventCore.Publish(evt);  // 自动归还到池

// 取消订阅
sub.Dispose();
```

### EventData

```csharp
public abstract class EventData : IPoolable
{
    public bool IsBreak { get; set; }  // 设为 true 中断后续监听器
    public abstract void OnRecycle();
}
```

### IEventListener<TEvent>

```csharp
public interface IEventListener<TEvent> where TEvent : EventData
{
    int Priority { get; }                          // 越大越先执行
    bool OnFilter(TEvent eventData);               // 返回 false 跳过
    void OnEvent(TEvent eventData);
}
```

## TimeCore — 时间轮定时器

基于环形槽位的时间轮算法。一个 TimeCore 实例对应一个独立的时间轮，由 `TimeWheel` 内部管理。

### 用法

```csharp
// 一次性定时器（1 秒后执行）
var timer = TimeCore.Once(1.0f, () => Debug.Log("1秒后执行"));

// 循环定时器（每 0.5 秒执行，共 10 次）
var timer = TimeCore.Loop(0.5f, () => Debug.Log("每0.5秒"), loopCount: 10);

// 控制
timer.Pause();
timer.Resume();
timer.Cancel();

// 驱动（Update 中调用）
TimeCore.Tick(Time.deltaTime);
```

## SpawnCore — 实体生成系统

管理工厂注册、实体生命周期和 ID 分配。所有生成的实体必须实现 `ISpawnable` 接口。

### 用法

```csharp
// 注册工厂（键名、生成回调、销毁回调）
SpawnCore.Register("enemy",
    (objectId, data) => new EnemyEntity { ObjectId = objectId },
    entity => { /* 清理资源 */ });

// 生成实体
var entity = SpawnCore.Spawn("enemy");

// 销毁实体
SpawnCore.Despawn(entity.ObjectId);

// 查询
var e = SpawnCore.GetEntity(entity.ObjectId);
var all = SpawnCore.GetAllEntities();
var enemies = SpawnCore.GetAllEntitiesOfType<EnemyEntity>();

// 生命周期事件
SpawnCore.OnEntitySpawned += entity => Debug.Log($"生成: {entity.ObjectId}");
SpawnCore.OnEntityDespawned += entity => Debug.Log($"销毁: {entity.ObjectId}");
```

### ISpawnable / ISpawnData

```csharp
public interface ISpawnable
{
    string SpawnKey { get; }  // 注册时的键值
    uint ObjectId { get; }    // 唯一 ID（SpawnCore 分配，只读）
}

// ISpawnData 是标记接口，用于携带初始化参数
public interface ISpawnData { }
```

## HttpCore — HTTP 请求封装

RESTful HTTP 请求，支持缓存、重试、并发节流。

### 用法

```csharp
// GET + 缓存
var result = await HttpCore.Get("https://api.example.com/data")
    .EnableCache()
    .SendAsync();
var data = result.GetJson<MyData>(LogCore);

// POST
var response = await HttpCore.Post("https://api.example.com/login")
    .SetBody(new { username = "admin", password = "123" })
    .SetTimeout(30f)
    .SetRetry(2)
    .SendAsync();

// 流式构建器
var builder = new HttpRequestBuilder(HttpMethod.Get, "https://api.example.com/users/123");
builder.SetHeader("Authorization", "Bearer xxx");
builder.SetQueryParam("fields", "name,email");
HttpResponse response = await builder.SendAsync();
```

## 注意事项

- **所有 Core 均为静态类**，不能继承或实例化。通过 `Setup()` 方法配置默认行为。
- **单线程模型**：除 `SaveCore` 和 `HttpCore` 的异步 API 外，所有 Core 均假设在主线程调用。
- **关闭顺序**：`GoveCore.Close()` 按严格逆序释放所有资源，无需手动调用各 Core 的 `Close()`。
- **`RandomCore` 和 `IRNG`** 位于 `Util/RNG/` 目录下，不依赖 Unity。
- **`HttpCore`** 必须在首次 HTTP 请求前调用 `HttpCore.Setup()`。
