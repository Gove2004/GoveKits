# Runtime/Util —— 基础工具库

面向游戏运行时的轻量基础能力：事件、日志、对象池、时间轮与通用工具，全部以静态 `*Core` 门面为统一入口。

## 模块架构

```
Runtime/Util/
├── Event/          事件系统 —— 池化事件 + 优先级/过滤/中断分发
├── Log/            日志系统 —— 等级过滤 + 多后端输出（Console/文件/自定义）
├── Pool/           对象池   —— C# 对象池 + GameObject 对象池
├── Time/           时间轮   —— 一次性/循环定时器，可暂停恢复取消
└── More/           工具集   —— DisposeAction / Bezier / RNG / Singleton / Spawn
```

## Event —— 事件系统

池化事件，发布自动回收零 GC；支持优先级（Priority 越大越先）、过滤（OnFilter）和中断（IsBreak）。

```csharp
// 1. 定义事件数据（继承 EventData，实现 OnRecycle 重置状态）
public class DamageEvent : EventData
{
    public int Damage;
    public int TargetId;
    public override void OnRecycle() { Damage = 0; TargetId = 0; IsBreak = false; }
}

// 2. 实现监听器（或用 Subscribe 的 IEventListener）
public class DamageLogger : IEventListener<DamageEvent>
{
    public int Priority => 0;                              // 越大越先执行
    public bool OnFilter(DamageEvent evt) => evt.Damage > 0;  // 返回 false 跳过
    public void OnEvent(DamageEvent evt) => LogCore.Info("战斗", $"目标{evt.TargetId} 受击 {evt.Damage}");
}

// 3. 订阅 / 取消订阅
var sub = EventCore.Subscribe<DamageEvent>(new DamageLogger());
sub.Dispose();                                            // 取消订阅

// 4. 发布（Pick 从池取，Publish 自动归还，禁止 new）
var evt = EventCore.Pick<DamageEvent>();
evt.Damage = 10; evt.TargetId = 3;
EventCore.Publish(evt);                                   // 发布后自动回收

// 中断传播：监听器内 evt.IsBreak = true 可阻止后续监听器执行
```

## Log —— 日志系统

统一日志入口，按等级过滤，可注入多个输出后端。

```csharp
// 基本用法（tag 标识来源模块，colorHex 控制 Console 颜色）
LogCore.Debug("模块A", "调试信息");
LogCore.Info("模块A", "运行消息");
LogCore.Warning("模块A", "潜在问题");
LogCore.Error("模块A", "功能异常");
LogCore.Success("模块A", "操作成功");          // 绿色
LogCore.Highlight("模块A", "重点消息");        // 青色
LogCore.Log("快捷日志");                        // 默认 Info + "Log" 标签

// 过滤：只输出 Warning 及以上（默认 Debug，全部输出）
LogCore.SetLogLevel(LogLevel.Warning);

// 自定义输出后端：实现 ILogger 即可
public class MyLogger : ILogger
{
    public void Log(LogLevel level, string tag, string message, string colorHex = null) { /* 发送到服务器 */ }
    public void Close() { }
}
LogCore.AddLogger(new MyLogger());              // 可同时挂多个后端

// 拦截日志（发送给后端之前触发）
LogCore.OnLog += (level, tag, msg, color) => { /* 采集分析 */ };
```

## Pool —— 对象池

C# 对象池（减少 GC 分配）与 GameObject 对象池（复用实例化）。

```csharp
// C# 对象池：对象实现 IPoolable，归还时自动 OnRecycle
public class BulletData : IPoolable
{
    public int Damage;
    public void OnRecycle() => Damage = 0;      // 归还时重置状态
}

var data = PoolCore.Get<BulletData>();          // 获取（自动创建/预热池）
data.Damage = 50;
PoolCore.Return(data);                          // 归还（自动 OnRecycle）
PoolCore.Clear<BulletData>();                   // 清理该类型池

// GameObject 对象池
var go = PoolCore.Get(bulletPrefab);            // 获取实例（自动激活）
// ... 使用后归还
PoolCore.Return(go);                            // 归还（自动失活，子物体 OnRecycle）
PoolCore.Clear(bulletPrefab);                   // 清理该预制体池

// 生命周期内统一关闭（通常由 GoveCore.Close 调用）
PoolCore.Close();
```

## Time —— 时间轮

批量定时器调度，适合冷却、Buff 倒计时、周期性任务；与 UniTask 互补（一次性异步流程用 UniTask，系统级调度用本模块）。

```csharp
// 初始化 + 每帧驱动（TimeCore.Setup 只调用一次，Tick 每帧调用）
TimeCore.Setup();                               // 默认 50ms 精度
void Update() => TimeCore.Tick(Time.deltaTime);

// 一次性定时
var t1 = TimeCore.Once(2f, () => LogCore.Log("2秒后触发"));

// 循环定时（无限循环 / 指定次数）
var t2 = TimeCore.Loop(1f, () => LogCore.Log("每秒一次"));        // loopCount=-1 无限
var t3 = TimeCore.Loop(0.5f, OnTick, 10);                        // 10 次后自动停止

// 暂停 / 恢复 / 取消
t2.Pause();                                     // 暂停倒计时
t2.Resume();                                    // 继续
t3.Cancel();                                    // 取消并回收
```

## More —— 工具集

### DisposeAction
把任意动作包装成 IDisposable，用于订阅凭证等场景。

```csharp
var sub = new DisposeAction(() => LogCore.Log("释放时执行"));
sub.Dispose();                                  // 触发动作
```

### Bezier
n 次贝塞尔曲线点位计算（Vector3 / Vector2）。

```csharp
Vector3[] points = { start, control, end };
Vector3 pos = Bezier.Calculate(0.5f, points);  // t ∈ [0,1]，曲线上的点
```

### RNG
可注入、可复现种子的随机数生成器（IRNG 接口 + NormalRNG 默认实现）。

```csharp
var rng = new NormalRNG(seed: 42);              // 同种子 = 同序列，可复现
int n = rng.Range(1, 100);                      // [1, 99] 整数
float f = rng.Range(0f, 1f);                    // [0, 1] 浮点
bool hit = rng.Chance(0.3f);                    // 30% 概率
var item = rng.Pick(itemList);                  // 等概率抽取
var drops = rng.PickMultiple(pool, 3);          // 无放回抽 3 个
var winner = rng.PickWeighted(items, x => x.Weight);  // 按权重轮盘赌
rng.Shuffle(cards);                             // 原地洗牌
float g = rng.NextGaussian(0f, 1f);             // 正态分布
```

### Singleton
CSharpSingleton（纯逻辑）与 MonoSingleton（挂 GameObject，DontDestroyOnLoad）。

```csharp
// 纯逻辑单例
public class GameConfig : CSharpSingleton<GameConfig>
{
    protected override void Init() { }          // 首次创建时调用
}
GameConfig.Instance;                            // 获取

// MonoBehaviour 单例（需要 Update/协程时用）
public class AudioManager : MonoSingleton<AudioManager>
{
    protected override void Init() { }          // 首次创建时调用
}
AudioManager.Instance;                          // 获取（场景中已有则复用，否则自动创建）
```

### Spawn
实体生成与销毁的统一管理（工厂注册 + 全局 ID + 生命周期事件）。

```csharp
// 1. 实体实现 ISpawnable
public class Enemy : ISpawnable
{
    public string SpawnKey => "enemy";
    public uint ObjectId { get; private set; }
}

// 2. 注册工厂与销毁回调
SpawnCore.Register("enemy",
    (id, data) => new Enemy { ObjectId = id },          // 工厂
    e => /* 清理资源 */);                                // 销毁回调

// 3. 生成 / 销毁 / 查询
var enemy = SpawnCore.Spawn("enemy");                   // 生成（自动分配 ID）
uint id = enemy.ObjectId;
SpawnCore.Despawn(id);                                  // 销毁（触发销毁回调 + OnEntityDespawned）
var e = SpawnCore.GetEntity(id);                        // 按 ID 查询
var all = SpawnCore.GetAllEntitiesOfType<Enemy>();      // 按类型查询

// 4. 生命周期事件
SpawnCore.OnEntitySpawned += e => { };
SpawnCore.OnEntityDespawned += e => { };
```
