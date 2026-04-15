# GoveKits Core 模块

GoveKits Runtime Core 提供了高性能、零GC、解耦的基础能力子系统，涵盖事件、日志、对象池、随机数、场景、单例、定时等通用功能。所有模块均可独立使用，支持工业级项目的高效开发。

## 目录结构

```
Core/
├── Event/         # 事件系统（解耦发布/订阅）
├── Log/           # 日志系统（多日志器、分级输出）
├── Pool/          # 极速对象池（C#对象/Unity对象）
├── Random/        # 可插拔随机数流
├── Scene/         # 场景管理工具
├── Singleton/     # 线程安全C#单例
├── Time/          # 高性能定时/时间轮
```

## 核心架构理念

1. **解耦与IoC**：各子系统均为静态入口，支持依赖注入与扩展，便于解耦业务逻辑。
2. **零GC与高性能**：对象池、定时器等核心模块采用Struct/池化设计，运行时无GC压力。
3. **易用性与可扩展**：API简洁，支持自定义扩展与替换底层实现。

## 子系统简介与用法

### 1. Event 事件系统

基于类型安全的发布/订阅，支持事件监听优先级、自动解绑。

```csharp
// 定义事件类型
public class DamageEvent : EventData { public int Value; }

// 订阅事件
EventBus bus = new EventBus();
bus.Subscribe<DamageEvent>(new MyListener());

// 发布事件
bus.Publish(new DamageEvent { Value = 100 });
```

### 2. Log 日志系统

支持多日志器注入、分级输出、彩色标签。

```csharp
// 注入自定义日志器
LogCore.InfuseLogger(new UnityLogger());

// 输出日志
LogCore.Debug("Core", "初始化完成");
LogCore.Info("Game", "游戏开始");
LogCore.Error("Net", "网络异常");
```

### 3. Pool 极速对象池

支持C#对象池与Unity GameObject池，极致复用，零GC。

```csharp
// 创建/获取C#对象池
var pool = PoolCore.Create<MyClass>(count: 8, maxSize: 64);
var obj = PoolCore.Get<MyClass>();
PoolCore.Release(obj);

// GameObject池用法类似
```

### 4. Random 随机数系统

可插拔RNG，支持自定义种子、流。

```csharp
RandomCore.Initialize(new NormalRNG(seed: 1234));
int value = RandomCore.NextInt(100); // 0~99
```

### 5. Scene 场景管理

便捷获取/判断场景状态。

```csharp
string name = SceneCore.ActiveSceneName;
bool isLoaded = SceneCore.IsSceneLoaded("Battle");
```

### 6. Singleton 单例基类

线程安全、延迟初始化的C#单例。

```csharp
public class MyManager : CSharpSingleton<MyManager> { }
var instance = MyManager.Instance;
```

### 7. Time 定时/时间轮

高性能时间轮，支持缩放/非缩放时间，定时器池化。

```csharp
// 初始化时间轮
TimeCore.Initialize();
// 每帧驱动
TimeCore.Update(Time.deltaTime, Time.unscaledDeltaTime);
// 创建一次性定时器
TimeCore.Once(1.5f, () => Debug.Log("1.5秒后触发"));
```

## 最佳实践与注意事项

1. **对象池复用**：优先通过PoolCore获取/释放对象，避免new/GC。
2. **事件解绑**：事件监听器建议用DisposeAction自动解绑，防止泄漏。
3. **日志分级**：合理设置LogCore.ShowLevel，避免生产环境输出Debug。
4. **定时器驱动**：TimeCore.Update需在主循环持续调用。
5. **扩展性**：各子系统均支持自定义实现与注入，满足复杂业务需求。

