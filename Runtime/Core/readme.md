# GoveKits Core 模块

GoveKits Runtime Core 是游戏开发的核心基础库，提供常用通用系统组件。采用静态入口设计，全局可访问，开箱即用。

## 目录结构

```
Core/
├── Event/           # 事件系统
├── Log/             # 日志系统
├── Pool/            # 对象池系统
├── Random/          # 随机系统
└── Singleton/       # 单例模式
```

## 1. Event 事件系统

发布 - 订阅模式的事件总线，支持优先级、过滤、中断，与对象池整合自动回收事件对象。

### 核心类

|类名|	说明|
|---|---|
|EventCore|	静态入口，提供 Publish/|Subscribe 方法|
|EventData|	事件数据基类，实现 IPoolable|
|IEventListener<T>|	监听器接口|
|ActionEventListener<T>|	Action 包装的简易监听器|
|DisposeAction|	取消订阅句柄，支持 using|

### 使用示例

```csharp
// 1. 定义事件类
public class DamageEvent : EventData
{
    public int damage;
    public string targetId;
    
    public override void OnRecycle()
    {
        damage = 0;
        targetId = null;
        IsBreak = false;
    }
}

// 2. 发布事件
EventCore.Publish<DamageEvent>(e =>
{
    e.damage = 50;
    e.targetId = "player";
});

// 3. 订阅事件（Action 方式）
// 推荐用法：将订阅与 MonoBehaviour 生命周期绑定
private IDisposable _damageSub;

void Start()
{
    _damageSub = EventCore.Subscribe<DamageEvent>(e => { /* ... */ });
}

void OnDestroy()
{
    _damageSub?.Dispose(); // 完美避免内存泄漏
}

// 4. 订阅事件（自定义监听器）
public class PlayerHealthListener : IEventListener<DamageEvent>
{
    public int Priority => 10;
    
    public bool OnFilter(DamageEvent e) => e.targetId == "player";
    
    public void OnEvent(DamageEvent e)
    {
        health -= e.damage;
    }
}

var listener = new PlayerHealthListener();
using (EventCore.Subscribe<DamageEvent>(listener)) { }

// 5. 中断后续监听器
EventCore.Publish<DamageEvent>(e =>
{
    e.IsBreak = true; // 后续监听器不再接收
});
```

### 注意事项

- 事件类必须继承 EventData 且有无参构造函数
- 必须重写 OnRecycle() 重置所有字段
- 使用 using 或手动 Dispose() 取消订阅，避免内存泄漏
- 不要在 OnEvent 中直接修改监听器列表
- 系统非线程安全，请在主线程使用

## 2. Log 日志系统

统一日志管理，支持多输出目标、等级过滤、颜色显示。

### 核心类

|类名|	说明|
|---|---|
|LogCore|	静态入口，提供 Debug/Info/Warn/Error 方法|
|LogLevel|	日志等级枚举|
|ILogger|	日志器接口|
|UnityLogger|	Unity 控制台输出|
|FileLogger|	文件输出|

### 使用示例

```csharp
// 1. 初始化：注入日志器
LogCore.InfuseLogger(new UnityLogger());
LogCore.InfuseLogger(new FileLogger("logs/game.log"));

// 2. 设置日志等级
LogCore.ShowLevel = LogLevel.Info; // 只显示 Info 及以上

// 3. 输出日志
LogCore.Debug("Game", "游戏启动");
LogCore.Info("Network", "连接服务器成功");
LogCore.Warn("Memory", "内存占用过高");
LogCore.Error("System", "发生严重错误");

// 4. 扩展方法
LogCore.Success("Achievement", "解锁成就！");      // 绿色
LogCore.Highlight("Event", "特殊事件触发");         // 青色

// 5. 自定义颜色
LogCore.Info("Custom", "自定义颜色", "#00ff00");
```

### 注意事项

- 建议在游戏启动时注入日志器
- ShowLevel 控制输出等级，数值越大越严重
- 日志器一旦注入无法删除
- Unity 富文本颜色格式：<color=#hex>文本</color>
- 文件日志需注意路径权限和磁盘空间

## 3. Pool 对象池系统

对象复用管理，减少运行时 GC 分配，支持 C# 对象和 GameObject。

### 核心类

|类名|	说明|
|---|---|
|PoolCore|	静态入口，提供 Create/Get/Return/Clear 方法|
|IPoolable|	可池化对象接口|
|CSharpPool<T>|	纯 C# 对象池|
|GameObjectPool|	GameObject 对象池|
|PoolRecord|	GameObject 池记录组件|

### 使用示例

```csharp
// ===== C# 对象池 =====

// 1. 定义可池化类
public class Bullet : IPoolable
{
    public int damage;
    public Vector3 direction;
    
    public void OnRecycle()
    {
        damage = 0;
        direction = Vector3.zero;
    }
}

// 2. 创建/预热池
PoolCore.Create<Bullet>(count: 10, maxSize: 50);

// 3. 获取对象
Bullet bullet = PoolCore.Get<Bullet>();
bullet.damage = 10;

// 4. 归还对象
PoolCore.Return(bullet);

// 5. 清空池
PoolCore.Clear<Bullet>();


// ===== GameObject 对象池 =====

// 1. 在预制体上挂载 IPoolable 组件
public class Enemy : MonoBehaviour, IPoolable
{
    public int hp = 100;
    
    public void OnRecycle()
    {
        hp = 100;
        // 重置其他状态
    }
}

// 2. 创建/预热池
PoolCore.Create(enemyPrefab, count: 5, maxSize: 20);

// 3. 获取实例
GameObject enemy = PoolCore.Get(enemyPrefab);

// 4. 归还实例
PoolCore.Return(enemy);

// 5. 清空所有池（场景切换时）
PoolCore.ClearAll();
```

### 注意事项

- 所有池化对象必须实现 IPoolable 接口
- 必须在 OnRecycle() 中重置所有字段
- 系统会自动为 GameObject 挂载 PoolRecord 组件，无需手动挂载，但请勿在运行时销毁该组件，否则无法正确归还。
- 超过 MaxSize 的对象会被销毁
- 场景切换时调用 PoolCore.ClearAll() 清理资源
- 系统非线程安全，请在主线程使用

## 4. Random 随机系统

确定性随机数生成，支持种子复现、独立流，线程安全。

### 核心类

|类名|	说明|
|---|---|
|RandomCore|	静态入口，提供各类随机方法|
|IRandomStream|	随机流接口|
|RandomStream|	随机流实现（线程安全）|

### 使用示例

```csharp
// 1. 初始化（必须在使用前调用）
RandomCore.Init(12345); // 固定种子可复现

// 2. 整数随机
int value1 = RandomCore.NextInt();           // 全范围
int value2 = RandomCore.NextInt(100);        // 0-99
int value3 = RandomCore.Range(1, 7);         // 1-6（骰子）

// 3. 浮点数随机
float f1 = RandomCore.NextFloat();           // 0.0-1.0
float f2 = RandomCore.Range(0.5f, 1.5f);     // 0.5-1.5

// 4. 概率判定
if (RandomCore.Chance(0.3f))                 // 30% 概率
{
    Debug.Log("暴击！");
}

// 5. 列表操作
string[] rewards = { "金币", "钻石", "装备" };
string reward = RandomCore.Pick(rewards);    // 随机选择一个

List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
RandomCore.Shuffle(numbers);                 // 打乱顺序

// 6. 独立随机流（不影响主随机流）
IRandomStream mapStream = RandomCore.CreateStream(99999);
int mapSeed = mapStream.NextInt();
float terrain = mapStream.Range(0f, 100f);
```

### 注意事项

- 使用前必须调用 RandomCore.Init()，否则 _defaultStream 为 null
- 固定种子用于测试/回放，时间种子用于正式运行
- 地图生成、掉落计算建议使用独立流，避免影响主随机序列
- 注意 maxExclusive 不包含上限，maxInclusive 包含上限
- 已加锁保护，支持多线程并发

## 5. Singleton 单例模式

提供两种单例实现基类，支持生命周期管理。

### 核心类

|类名|	说明|
|---|---|
|CSharpSingleton<T>|	纯 C# 单例，线程安全|
|MonoSingleton<T>|	Unity 组件单例，自动持久化|

### 使用示例

```csharp
// ===== C# 单例 =====

public class GameManager : CSharpSingleton<GameManager>
{
    private int score;
    
    protected override void Init()
    {
        score = 0;
        Debug.Log("GameManager 初始化");
    }
    
    protected override void Uninit()
    {
        Debug.Log("GameManager 清理");
    }
    
    public void AddScore(int points) => score += points;
}

// 访问
GameManager.Instance.AddScore(100);

// 销毁实例
GameManager.DestroyInstance();


// ===== Unity 单例 =====

public class AudioManager : MonoSingleton<AudioManager>
{
    protected override void Init()
    {
        Debug.Log("AudioManager 初始化");
    }
    
    protected override void Uninit()
    {
        Debug.Log("AudioManager 清理");
    }
    
    public void PlayMusic(string clipName) { }
}

// 访问（自动创建 GameObject 并持久化）
AudioManager.Instance.PlayMusic("BGM");
```

### 注意事项

- CSharpSingleton 要求泛型类型有无参构造函数（new() 约束）
- MonoSingleton 在编辑器模式下不会自动创建实例
- MonoSingleton 自动 DontDestroyOnLoad，跨场景持久化
- 两种单例都支持重写 Init() 和 Uninit() 生命周期方法
- CSharpSingleton 线程安全，MonoSingleton 仅主线程使用

## 通用注意事项

### 初始化

|模块|	是否需要手动初始化|
|---|---|
|RandomCore|	✅ 必须调用 Init()|
|LogCore|	✅ 建议注入日志器|
|EventCore|	❌ 懒加载|
|PoolCore|	❌ 懒加载|
|Singleton|	❌ 懒加载|
