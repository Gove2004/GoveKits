# Util 模块

> 命名空间：`GoveKits.Runtime.Util` | 位置：`Runtime/Util/`
>
> 基础能力集合：日志、事件、对象池、时间调度、实体生成与通用工具。不依赖任何其他 GoveKits 模块，由 `GoveCore.Setup()` 自动就绪。

## Event 事件系统

强类型 + 池化的事件总线：事件对象复用对象池，发布零 GC；支持优先级排序、过滤、中断传播。

**定义事件**（继承 `EventData`）：

```csharp
public class DamageEvent : EventData
{
    public int TargetId;
    public int Amount;

    // 归还池时调用，必须重置所有字段，防止复用脏数据
    public override void OnRecycle()
    {
        TargetId = 0;
        Amount = 0;
    }
}
```

**发布**（Get 与 Publish 配对，框架自动池化）：

```csharp
var evt = EventCore.Pick<DamageEvent>();   // 从池取，不要 new
evt.TargetId = enemy.Id;
evt.Amount = 50;
EventCore.Publish(evt);                        // 分发后自动归还池
```

**订阅**（实现 `IEventListener<T>`，返回取消凭证）：

```csharp
public class CombatListener : IEventListener<DamageEvent>
{
    public int Priority => 100;                              // 越大越先执行
    public bool OnFilter(DamageEvent e) => e.Amount > 0;     // 返回 false 则跳过
    public void OnEvent(DamageEvent e) { /* 处理 */ }
}

IDisposable handle = EventCore.Subscribe(new CombatListener());
handle.Dispose();   // 取消订阅
```

**要点**
- 事件对象一律走 `Pick` / `Publish`，禁止 `new`、禁止手动归还
- `OnRecycle` 重置所有字段（引用类型置 null），否则会读到上一条事件的脏数据
- 监听器中把 `eventData.IsBreak = true` 可中断后续监听器
- 高频事件（伤害/击杀/状态变化）优先用它；低频跨模块广播也适用

## Log 日志

统一日志入口，等级过滤 + 多后端分发。框架内日志一律走 `LogCore`。

**基础用法**：

```csharp
LogCore.SetLogLevel(LogLevel.Debug);          // 低于此等级的不输出

LogCore.Verbose("Tag", "琐碎信息");
LogCore.Debug("Tag", "流程跟踪");
LogCore.Info("Battle", "回合开始");
LogCore.Warning("Config", $"缺少配置: {id}");
LogCore.Error("Network", "连接超时");

LogCore.Log("无标签快捷日志");
LogCore.Success("Save", "存档完成");           // 绿色
LogCore.Highlight("UI", "重点信息");           // 青色
LogCore.Temp("Dev", "临时调试");               // 紫色
```

**自定义后端**（实现 `ILogger`，多个后端并存）：

```csharp
public class MyLogger : ILogger
{
    public void Log(LogLevel level, string tag, string message, string colorHex = null)
    {
        // colorHex 为富文本颜色（文件等后端可忽略）
    }
    public void Close() { }   // 释放文件句柄等资源
}

LogCore.AddLogger(new MyLogger());
LogCore.AddLogger(new FileLogger(Path.Combine(Application.persistentDataPath, "log.txt")));
```

**要点**
- 业务代码不直接 `Debug.Log`，统一走 `LogCore` 便于等级过滤与多端输出
- 单个后端抛异常不影响其他后端（已隔离）
- `OnLog` 事件可在分发前拦截/采集（如上报服务器）

## Pool 对象池

双池架构：C# 纯托管对象池 + GameObject 池，降低高频创建/销毁的 GC 与实例化开销。

**C# 对象池**（实现 `IPoolable` 的托管对象）：

```csharp
public class BulletData : IPoolable
{
    public Vector3 Dir;

    public void OnRecycle() { Dir = Vector3.zero; }   // 归还时重置
}

var data = PoolCore.Get<BulletData>();    // 首次调用自动建池 + 预热 8 个
PoolCore.Return(data);                    // 归还，自动调 OnRecycle
```

**GameObject 池**（预制体复用，归还自动归还原池）：

```csharp
GameObjectPool pool = PoolCore.Create(bulletPrefab, count: 16, maxSize: 64);
GameObject go = PoolCore.Get(bulletPrefab);   // 池空自动实例化
PoolCore.Return(go);                          // 自动归还原池；无归属则销毁
```

**要点**
- 池化对象归还时自动调 `OnRecycle`，必须在里面重置状态
- 同类型/同预制体只建一个池，重复 `Create` 拿的是已有池
- 超出 `maxSize` 的对象在归还时被销毁（不保留）
- 弹幕、特效、飘字、纯数据对象适合池化；归还时子物体上的 `IPoolable` 会被递归调用

## Time 定时调度

基于时间轮的批量定时器，Timer 对象池化。适合冷却、Buff、周期任务等**需要统一管理**的调度场景。

```csharp
// 一次性：2 秒后触发一次
Timer t1 = TimeCore.Once(2f, () => LogCore.Log("2秒后触发"));

// 循环：每 0.5 秒一次，共 3 次（loopCount = -1 无限循环）
Timer t2 = TimeCore.Loop(0.5f, () => LogCore.Log("tick"), loopCount: 3);

// 控制：暂停 / 恢复 / 取消
t1.Pause();
t1.Resume();
t1.Cancel();
```

**每帧驱动**（时间轮需要推进，放在任意 `MonoBehaviour.Update`）：

```csharp
void Update() => TimeCore.Tick(Time.deltaTime);
```

**要点**
- 创建的 `Timer` 务必持有引用，否则无法中途 `Cancel`
- 一次性异步流程（等 2 秒做某事）用 UniTask，冷却/周期/Buff 用 `TimeCore`，各司其职
- 回调里再创建定时器是安全的（内部已做延迟插入处理）
- 单定时器回调抛异常不影响其他定时器

## Spawn 实体生成

工厂注册制管理业务实体的生成与销毁，实体持有全局唯一 `ObjectId`。

```csharp
// 启动时注册（同 key 重复注册会覆盖并告警）
SpawnCore.Register("Enemy",
    (id, data) => new EnemyEntity(id, (EnemySpawnData)data),   // 工厂
    e => ((EnemyEntity)e).Release());                          // 销毁回调

// 生成 / 销毁 / 查询
ISpawnable entity = SpawnCore.Spawn("Enemy", new EnemySpawnData { Hp = 100 });
uint id = entity.ObjectId;
SpawnCore.Despawn(id);
ISpawnable found = SpawnCore.GetEntity(id);

// 遍历存活实体
List<EnemyEntity> enemies = SpawnCore.GetAllEntitiesOfType<EnemyEntity>();

// 生命周期事件
SpawnCore.OnEntitySpawned += e => { /* 实体生成 */ };
SpawnCore.OnEntityDespawned += e => { /* 实体销毁 */ };
```

**要点**
- 与 Pool 的分工：**Pool 管渲染对象复用**（弹幕/特效），**Spawn 管业务实体**（带 ID、有工厂与销毁逻辑）
- `Spawn` 可传 `predefinedId` 用于服务器权威的 ID 同步
- `Despawn` 幂等，重复销毁仅告警

## More 通用工具

无门面的独立工具，按需取用：

```csharp
// DisposeAction —— 订阅凭证等场景，Dispose 时执行回调（struct，零闭包分配）
IDisposable handle = new DisposeAction(() => LogCore.Log("disposed"));

// Bezier —— n 次贝塞尔曲线点位
Vector3 pos = Bezier.Calculate(0.5f, p0, p1, p2);

// IRNG / NormalRNG —— 可注入随机数，种子可控可复现
IRNG rng = new NormalRNG(seed: 42);       // 可复现场景用独立实例
rng.Reseed(20260827);                     // 重设种子重置序列
int n = rng.Range(0, 100);
float g = rng.NextGaussian(mean: 0f, stdDev: 1f);
var picked = rng.Pick(weaponList);        // 等概率抽取
var subset = rng.PickMultiple(monsterList, 3);   // 无放回抽取
var winner = rng.PickWeighted(items, i => i.Weight);  // 权重轮盘赌
rng.Shuffle(cards);                       // Fisher-Yates 洗牌

// CSharpSingleton —— 纯 C# 单例
public class GameConfig : CSharpSingleton<GameConfig>
{
    protected override void Init() { }     // 首次创建后调用一次
}
GameConfig.Instance.DoSomething();

// MonoSingleton —— MonoBehaviour 单例，自动挂载 DontDestroyOnLoad 容器
public class AudioManager : MonoSingleton<AudioManager>
{
    protected override void Init() { }     // Awake 中首次调用
}
AudioManager.Instance.PlayBgm();
```

**要点**
- 可复现场景（肉鸽生成、战斗随机）用 `NormalRNG` 独立实例 + 种子；不要依赖 `UnityEngine.Random`（全局序列不可控）
- 纯逻辑用 `CSharpSingleton`，需要 MonoBehaviour 生命周期（Update/协程）才用 `MonoSingleton`
- `DisposeAction` 是 struct，`EventCore.Subscribe` 返回的即是它，无需手动释放

## 与外部库的分工

| 库 | 分工 |
|---|---|
| UniTask | 一次性异步流程（等待、协程替代），与 `TimeCore` 互补 |
| DOTween | 补间动画，与 `Bezier`（几何曲线）用途不同 |
| UnityEngine.Random | 非种子可控的临时随机，正式玩法逻辑用 `IRNG` |
