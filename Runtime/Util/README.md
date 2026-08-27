# Util 模块

> 命名空间：`GoveKits.Runtime.Util` | 位置：`Runtime/Util/`

GoveKits 的基础能力集合，涵盖日志、事件、对象池、时间调度、实体生成与通用工具。**Util 是框架的地基，不依赖任何其他 GoveKits 模块**，Storage / Network / Unit / UI 均建立在其之上。

---

## 一、模块架构

### 统一范式：门面 + 接口 + 实现

Util 内每个子系统都遵循同一套结构，学习一个即可类推其余：

```
XxxCore.cs       静态门面 —— 唯一对外入口，管理全局状态与生命周期
IXxx.cs          扩展接口 —— 新增实现 = 新增能力（框架的扩展点）
默认实现类        框架内置，开箱即用
```

| 子模块 | 门面 | 扩展接口 | 内置实现 | 职责 |
|---|---|---|---|---|
| Event 事件 | `EventCore` | `IEventListener<T>` | `EventBus` / `EventData` | 强类型事件 + 池化发布订阅 |
| Log 日志 | `LogCore` | `ILogger` | `UnityLogger` / `FileLogger` | 等级过滤 + 多后端输出 |
| Pool 对象池 | `PoolCore` | `IPool<T>` / `IPoolable` | `CSharpPool` / `GameObjectPool` | C# 对象与 GameObject 复用 |
| Time 定时 | `TimeCore` | —（内置调度器） | `TimeWheel` / `Timer` | 高精度批量定时器 |
| Spawn 实体 | `SpawnCore` | `ISpawnable` / `ISpawnData` | 工厂注册制 | 业务实体生成与销毁 |
| More 工具 | —（独立静态工具） | `IRNG` | `DisposeAction` / `Bezier` / `NormalRNG` / `CSharpSingleton` / `MonoSingleton` | 零散通用工具 |

### 生命周期

四个带门面的子系统统一由 `GoveCore` 驱动。仅 `TimeCore` 需显式 `Setup()`（其余为惰性就绪，首次使用自动创建），关闭时逆序清理：

```
GoveCore.Setup()      →  TimeCore.Setup()（EventCore/LogCore/PoolCore 惰性就绪）
游戏运行每帧          →  TimeCore.Tick(deltaTime) 驱动时间轮
GoveCore.Close()      →  逆序清理：SpawnCore → TimeCore → EventCore → PoolCore → LogCore
```

> 调用约定：**Close 之后系统失效，禁止再调用**（各 Core 已做置 null / 判空防御）。

---

## 二、子模块详解

### 1. Event 事件系统

强类型、池化、可中断的事件总线。事件对象复用对象池，发布零 GC。

**定义事件**（继承 `EventData`，归还池时重置状态）：

```csharp
public class DamageEvent : EventData
{
    public int TargetId;
    public int Amount;

    public override void OnRecycle()   // 必须重置全部字段，防止脏数据复用
    {
        TargetId = 0;
        Amount = 0;
    }
}
```

**发布**（Get 与 Publish 配对，框架自动池化）：

```csharp
var evt = EventCore.GetEvent<DamageEvent>();
evt.TargetId = enemy.Id;
evt.Amount = 50;
EventCore.Publish(evt);          // 分发后自动归还池
```

**订阅**（实现 `IEventListener<T>`，返回 `IDisposable` 凭证）：

```csharp
public class CombatListener : IEventListener<DamageEvent>
{
    public int Priority => 100;                      // 越大越先执行
    public bool OnFilter(DamageEvent e) => e.Amount > 0;  // 过滤
    public void OnEvent(DamageEvent e) { /* 处理 */ }
}

IDisposable handle = EventCore.Subscribe(new CombatListener());
handle.Dispose();                // 取消订阅
```

**特性**：`Priority` 优先级排序、`OnFilter` 过滤、`IsBreak` 中断传播（设为 true 跳过后续监听器）。

**最佳实践**
- 事件对象**必须**通过 `GetEvent` 获取、由 `Publish` 归还，禁止 `new` 或手动归还，否则池状态错乱
- `OnRecycle` 必须重置**所有**字段（含引用类型置 null），否则会读到上一条事件的脏数据
- 高频事件（伤害、击杀、状态变化）优先用本系统；低频跨场景广播可用订阅者自持监听器
- 需要"编辑器可视化的全局事件"时，在外部包一层 SO 适配（SO 资产 → 内部调 `EventCore.Publish`），不要替换本系统

---

### 2. Log 日志

统一日志入口，等级过滤 + 多后端分发。框架内所有模块的日志一律走 `LogCore`，杜绝直接 `Debug.Log` 散落各处。

**等级**：`Verbose < Debug < Info < Warning < Error`，`SetLogLevel` 设置最低输出等级。

```csharp
LogCore.SetLogLevel(LogLevel.Debug);
LogCore.Info("Battle", "回合开始");
LogCore.Warning("Config", $"缺少配置: {id}");
LogCore.Error("Network", "连接超时");
LogCore.Success("Save", "存档完成");      // 便捷方法：绿色 Info
```

**自定义后端**（框架扩展点）：实现 `ILogger` 即可接入任意输出目标（控制台、文件、远程服务器）：

```csharp
public class MyLogger : ILogger
{
    public void Log(LogLevel level, string tag, string message, string colorHex = null) { /* ... */ }
    public void Close() { /* 释放资源 */ }
}

LogCore.AddLogger(new MyLogger());       // 多个后端可并存
LogCore.AddLogger(new FileLogger(Path.Combine(Application.persistentDataPath, "log.txt")));
```

**特性**：`OnLog` 事件可在分发前拦截/采集日志；单个后端抛异常**不影响其他后端**（已做隔离）。

**最佳实践**
- 日志统一走 `LogCore`，业务代码不直接 `Debug.Log`
- 登录/支付/战斗等关键节点用 `LogCore.Info` + 标签，便于日志检索
- 文件日志后端适合真机问题排查，注意高频路径（如每帧日志）慎用（当前为逐条写文件）

---

### 3. Pool 对象池

双池架构：C# 纯托管对象池 + GameObject 池，高频创建/销毁场景的 GC 与实例化开销优化。

**C# 对象池**（`IPoolable` 对象复用）：

```csharp
public class BulletData : IPoolable
{
    public Vector3 Dir;
    public void OnRecycle() { Dir = Vector3.zero; }
}

var bullet = PoolCore.Get<BulletData>();    // 自动建池 + 预热
PoolCore.Return(bullet);                    // 归还，自动 OnRecycle
```

**GameObject 池**（预制体复用，带 `PoolRecord` 自动追踪归属）：

```csharp
GameObjectPool pool = PoolCore.Create(bulletPrefab, count: 16, maxSize: 64);
GameObject go = PoolCore.Get(bulletPrefab);    // 池空自动实例化
PoolCore.Return(go);                           // 自动归还原池；无归属则销毁
```

**特性**：`Warmup` 预热、`Capacity` 容量上限（超出销毁）、归还时递归调用子物体 `OnRecycle`、`Clear/Close` 批量清理。

**最佳实践**
- 需要池化的类型：纯数据对象、弹幕/特效/飘字、高频临时实体
- `OnRecycle` 必须重置状态，**不能依赖池外部清理**
- 单类型单池：`Create<T>` 同类型复用同一实例，二次调用拿到的就是已有池
- 不同预制体独立建池，同一预制体的多个实例共享一个池

---

### 4. Time 定时调度

基于**时间轮**（环形槽位 + 链表）的高精度批量定时器，Timer 对象池化，零 GC。适合游戏内的冷却、Buff、周期任务等**需要统一管理**的调度场景。

```csharp
// 一次性
Timer t1 = TimeCore.Once(2f, () => LogCore.Log("2秒后触发"));

// 循环（每 0.5 秒一次，共 3 次；loopCount = -1 无限循环）
Timer t2 = TimeCore.Loop(0.5f, () => LogCore.Log("tick"), loopCount: 3);

// 控制
t1.Pause();    t1.Resume();    t1.Cancel();
```

**驱动**：时间轮需要每帧推进，在任意 `MonoBehaviour.Update` 中调用 `TimeCore.Tick(Time.deltaTime)`（或由宿主框架的全局驱动调用）。

**特性**：暂停/恢复/取消、循环次数控制、回调异常隔离（单定时器异常不影响其他）、`Timer` 由 `PoolCore` 池化管理。

**最佳实践**
- **冷却/Buff/周期任务 → `TimeCore`**；**一次性异步流程 → `UniTask`**（见"与外部库的关系"），各司其职
- 创建的 `Timer` 务必持有引用，方便后续 `Cancel`；不持有 = 无法中途取消
- 回调内创建新定时器是安全的（时间轮已做 pending 延迟插入处理）
- `TimeCore.Setup()` 在 `GoveCore.Setup()` 中完成，无需手动调用

---

### 5. Spawn 实体生成

以"工厂注册"模式统一管理业务实体的生成与销毁，每个实体持有全局唯一 `ObjectId`。

```csharp
// 注册（启动时一次性）
SpawnCore.Register("Enemy",
    (id, data) => new EnemyEntity(id, (SpawnData)data),   // 工厂
    e => ((EnemyEntity)e).Release());                     // 销毁回调

// 生成 / 销毁 / 查询
ISpawnable e = SpawnCore.Spawn("Enemy", new SpawnData { Hp = 100 });
uint id = e.ObjectId;
SpawnCore.Despawn(id);
ISpawnable found = SpawnCore.GetEntity(id);
```

**特性**：`OnEntitySpawned/OnEntityDespawned` 生命周期事件、`GetAllEntities`/`GetAllEntitiesOfType<T>` 遍历存活实体、`predefinedId` 支持服务器权威的 ID 同步。

**最佳实践**
- 与 Pool 的分工：**Pool 管渲染对象的复用**（弹幕/特效），**Spawn 管业务实体**（带 ID、有工厂与销毁逻辑的玩法对象）
- `Register` 同 key 重复注册会覆盖并告警，全局注册建议集中在启动脚本
- `Despawn` 幂等（重复销毁已存在实体仅告警）

---

### 6. More 通用工具

无门面的独立工具集，按需取用：

| 工具 | 说明 |
|---|---|
| `DisposeAction` | 轻量 `IDisposable`，包装一个 Action，Dispose 时执行。用于订阅凭证等场景，避免 lambda 闭包分配 |
| `Bezier` | n 次贝塞尔曲线点位计算（Vector3/Vector2 重载） |
| `IRNG` / `NormalRNG` | 可注入随机数接口 + 标准实现（可 `Reseed` 复现序列；正态分布/权重抽取/洗牌/无放回抽取）。**单线程环境使用** |
| `CSharpSingleton<T>` | 纯 C# 单例基类，惰性创建 + `Init/Uninit` 钩子 |
| `MonoSingleton<T>` | MonoBehaviour 单例基类，自动挂载到 `DontDestroyOnLoad` 容器，场景重复实例自动销毁 |
| `ISpawnable` / `ISpawnData` | Spawn 模块的数据与实体接口 |

**最佳实践**
- **RNG**：可复现场景（肉鸽地图生成、战斗随机）用 `NormalRNG(seed)` 独立实例；需要种子可控就 `Reseed`，不要依赖 `UnityEngine.Random` 的全局序列
- **Singleton 分工**：纯逻辑单例用 `CSharpSingleton`；需要 MonoBehaviour 生命周期（Update/协程）才用 `MonoSingleton`；**能用静态 `*Core` 门面解决的不造单例**
- `DisposeAction` 是 struct，随 `EventCore.Subscribe` 返回，无需手动释放

---

## 三、与外部库的关系

| 库 | 分工 |
|---|---|
| **UniTask** | 一次性异步流程（等待、协程替代），与 TimeCore 互补 |
| **DOTween** | 补间动画，与 Bezier（几何曲线）用途不同 |
| **UnityEngine.Random** | 非种子可控的临时随机，正式玩法逻辑用 `IRNG` |

Util 模块**零外部依赖**（仅 UnityEngine 基础 API），可独立移植。

## 四、快速上手

```csharp
// GoveCore.Setup() 已自动完成 Util 全部初始化，业务侧零配置

// 1. 记日志
LogCore.Info("Main", "游戏启动");

// 2. 发事件
var evt = EventCore.GetEvent<DamageEvent>();
evt.TargetId = 1; evt.Amount = 10;
EventCore.Publish(evt);

// 3. 池化复用
var data = PoolCore.Get<BulletData>();

// 4. 定时任务
Timer cd = TimeCore.Once(3f, () => LogCore.Log("技能冷却结束"));

// 5. 每帧驱动时间轮（宿主 MonoBehaviour.Update 中）
TimeCore.Tick(Time.deltaTime);
```
