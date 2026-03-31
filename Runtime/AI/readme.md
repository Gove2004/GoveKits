# GoveKits AI 模块

GoveKits Runtime AI 是游戏 AI 开发框架，采用 感知 - 记忆 - 思考 - 执行 的闭环架构设计，提供组件化 AI 系统、有限状态机、黑板记忆等功能。开箱即用，与 Core 模块深度整合。

## 目录结构

```
AI/
├── AIBrain.cs           # AI 核心接口定义
├── AIActor.cs           # AI 行动者基类
└── FSM/
    ├── KVMemory.cs      # 键值对记忆实现
    ├── FSMState.cs      # FSM 状态基类
    └── FSMTinker.cs     # FSM 思考者实现
```

## 架构设计

```
┌─────────────────────────────────────────────────────────┐
│                       AIActor                           │
│  • 统筹 感知→记忆→思考→执行 闭环  • 管理组件生命周期        │
└─────────────────────────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   IAIObserver   │ │   IAIMemory     │ │   IAITinker     │
│  • 观察世界      │ │  • 存储数据     │ │  • 做出决策      │
│  • 写入记忆      │ │  • 读写接口     │ │  • 输出意图      │
└─────────────────┘ └─────────────────┘ └─────────────────┘
                           │                       │
                           ▼                       ▼
                   ┌───────────────┐       ┌───────────────┐
                   │   KVMemory    │       │  FSMTinker    │
                   │  • 键值存储    │       │  • 状态管理   │
                   └───────────────┘       │  • 状态跳转    │
                                           └───────────────┘
                                                   │
                                                   ▼
                                           ┌───────────────┐
                                           │   FSMState    │
                                           │  • OnEnter    │
                                           │  • OnUpdate   │
                                           │  • OnExit     │
                                           └───────────────┘
```

## 1. AIBrain 核心接口

定义 AI 系统的三大核心接口，采用接口隔离模式，支持多种实现自由组合。

### 核心接口

| 接口名 | 说明 |
|---|---|
| IAIMemory | 记忆接口 - 存储 AI 感知到的世界数据 |
| IAIObserver | 感知器接口 - 观察世界并写入记忆 |
| IAITinker | 思考者接口 - 根据记忆做出决策 |

### 接口职责

```
┌─────────────────────────────────────────────────────────┐
│                    AI 核心三角                           │
│                                                         │
│   ┌─────────────┐      写入      ┌─────────────┐       │
│   │  Observer   │ ────────────→  │   Memory    │       │
│   │  (感知器)   │                │   (记忆)    │       │
│   └─────────────┘                └──────┬──────┘       │
│                                         │ 读取          │
│                                         ▼               │
│                                  ┌─────────────┐       │
│                                  │   Tinker    │       │
│                                  │  (思考者)   │       │
│                                  └──────┬──────┘       │
│                                         │ 输出意图      │
│                                         ▼               │
│                                  ┌─────────────┐       │
│                                  │    Actor    │       │
│                                  │  (执行者)   │       │
│                                  └─────────────┘       │
└─────────────────────────────────────────────────────────┘
```

### 使用示例

```csharp
// ===== 1. 自定义记忆实现 =====
public class BlackboardMemory : IAIMemory
{
    private Dictionary<string, object> _data = new();
    
    public void Init() => _data.Clear();
    public void UnInit() => _data.Clear();
    public void Set<T>(string key, T value) => _data[key] = value;
    public T Get<T>(string key) => _data.TryGetValue(key, out var v) ? (T)v : default;
}

// ===== 2. 自定义感知器 =====
public class VisionObserver : IAIObserver
{
    private float _viewRange = 10f;
    
    public void Init() { }
    public void UnInit() { }
    
    public void Observe(IAIMemory memory)
    {
        // 检测视野内的敌人
        var enemies = Physics.OverlapSphere(transform.position, _viewRange);
        memory.Set("EnemyNearby", enemies.Length > 0);
        memory.Set("EnemyCount", enemies.Length);
    }
}

// ===== 3. 自定义思考者 =====
public class BehaviorTreeTinker : IAITinker
{
    private BehaviorTree _tree;
    
    public void Init() => _tree = new BehaviorTree();
    public void UnInit() => _tree = null;
    
    public string Think(IAIMemory memory)
    {
        _tree.Update(memory);
        return _tree.CurrentAction;
    }
}
```

### 注意事项

- 接口设计支持多种实现自由替换（FSM、行为树、GOAP 等）
- Memory 是 Observer 和 Tinker 之间的数据桥梁
- 所有接口的 Init/UnInit 必须成对调用
- 接口方法应保持轻量，避免在 Think/Observe 中执行耗时操作

## 2. AIActor 行动者

AI 系统的最终承载者，统筹感知→记忆→思考→执行的完整闭环。

### 生命周期

```
Start() → Init() → SetupAI() → 装配组件 → 级联初始化
   │
   ▼
Update() → TickAI() → Observe() → Think() → Act()
   │
   ▼
OnDestroy() → UnInit() → 逆序清理 → 清空引用
```

### 使用示例

```csharp
// ===== 1. 继承 AIActor =====
public class EnemyActor : AIActor
{
    protected override void SetupAI(out IAIMemory memory, out IAITinker tinker, out List<IAIObserver> observers)
    {
        // 装配记忆系统
        memory = new KVMemory();
        
        // 装配思考系统（FSM）
        var fsm = new FSMTinker();
        fsm.AddState(new IdleState());
        fsm.AddState(new PatrolState());
        fsm.AddState(new AttackState());
        fsm.SetInitialState("Idle");
        tinker = fsm;
        
        // 装配感知器
        observers = new List<IAIObserver>
        {
            new VisionObserver(),
            new HearingObserver()
        };
    }
    
    protected override void Act(string intendedAction)
    {
        // 将抽象意图转化为具体行为
        switch (intendedAction)
        {
            case "Idle":
                // 播放待机动画
                break;
            case "Patrol":
                // 执行巡逻移动
                break;
            case "Attack":
                // 执行攻击动作
                break;
            case "Flee":
                // 执行逃跑逻辑
                break;
        }
    }
}

// ===== 2. 手动控制生命周期 =====
// 一般不需要手动调用，AIActor 会自动绑定 MonoBehaviour 生命周期
var enemy = gameObject.AddComponent<EnemyActor>();
enemy.Init();    // 手动初始化（可选）
enemy.UnInit();  // 手动清理（可选）
```

### 注意事项

- 必须在 SetupAI 中装配所有 AI 组件
- Act 方法必须实现，将意图转化为具体行为
- 生命周期与 MonoBehaviour 自动绑定（Start/Update/OnDestroy）
- 避免在 TickAI 中执行耗时操作，保持每帧性能
- Observers 列表可为空，但 Memory 和 Tinker 必须提供

## 3. KVMemory 记忆实现

基于键值对的通用记忆体实现，作为 IAIMemory 接口的默认实现。

### 核心特性

| 特性 | 说明 |
|---|---|
| 泛型存储 | 支持任意类型的数据存取 |
| 键值索引 | 按字符串键名快速查找 |
| 轻量级 | 基于 Dictionary<string, object> |

### 使用示例

```csharp
// ===== 1. 基本读写 =====
var memory = new KVMemory();
memory.Init();

// 写入数据
memory.Set("EnemyHP", 100);
memory.Set("LastSeenPos", new Vector3(1, 2, 3));
memory.Set("IsAlert", true);

// 读取数据
int hp = memory.Get<int>("EnemyHP");
Vector3 pos = memory.Get<Vector3>("LastSeenPos");
bool alert = memory.Get<bool>("IsAlert");

// 读取不存在的数据返回 default
int unknown = memory.Get<int>("UnknownKey"); // 返回 0

// ===== 2. 在 Observer 中使用 =====
public class VisionObserver : IAIObserver
{
    public void Observe(IAIMemory memory)
    {
        var target = FindTarget();
        if (target != null)
        {
            memory.Set("TargetVisible", true);
            memory.Set("TargetPosition", target.transform.position);
            memory.Set("TargetDistance", Vector3.Distance(transform.position, target.transform.position));
        }
        else
        {
            memory.Set("TargetVisible", false);
        }
    }
}

// ===== 3. 在 Tinker 中使用 =====
public class FSMTinker : IAITinker
{
    public string Think(IAIMemory memory)
    {
        bool enemyVisible = memory.Get<bool>("TargetVisible");
        float distance = memory.Get<float>("TargetDistance");
        
        if (enemyVisible && distance < 5f)
            return "Attack";
        else if (enemyVisible)
            return "Chase";
        else
            return "Patrol";
    }
}
```

### 注意事项

- Get 时需要指定正确的类型，否则可能抛出异常
- 键名建议使用常量或枚举，避免硬编码字符串
- 存储引用类型时注意内存管理
- Init/UnInit 会清空所有数据，谨慎调用

## 4. FSMState 状态基类

有限状态机的状态基类，定义状态的生命周期方法。

### 核心方法

| 方法名 | 调用时机 | 说明 |
|---|---|---|
| OnEnter | 进入状态时 | 初始化状态数据 |
| OnUpdate | 每帧 | 执行状态逻辑，可请求跳转 |
| OnExit | 退出状态时 | 清理状态数据 |

### 状态生命周期

```
          ┌─────────────┐
          │   OnEnter   │ ← 进入状态时调用一次
          └──────┬──────┘
                 │
          ┌──────▼──────┐
     ┌───→│   OnUpdate  │ ← 每帧调用
     │    └──────┬──────┘
     │           │
     │    ┌──────▼──────┐
     │    │  nextState? │
     │    └──────┬──────┘
     │           │
     │      ┌────┴────┐
     │      │  跳转？  │
     │      └────┬────┘
     │           │
     │    ┌──────▼──────┐
     └────│    OnExit   │ ← 退出状态时调用一次
          └─────────────┘
```

### 使用示例

```csharp
// ===== 1. 待机状态 =====
public class IdleState : FSMState
{
    private float _idleTimer = 0f;
    
    public IdleState() => StateName = "Idle";
    
    public override void OnEnter(IAIMemory memory)
    {
        _idleTimer = 0f;
        // 播放待机动画
    }
    
    public override string OnUpdate(IAIMemory memory, out string nextState)
    {
        nextState = null;
        _idleTimer += Time.deltaTime;
        
        // 待机 3 秒后自动切换到巡逻
        if (_idleTimer >= 3f)
        {
            nextState = "Patrol";
        }
        
        // 发现敌人则切换到攻击
        if (memory.Get<bool>("EnemySpotted"))
        {
            nextState = "Attack";
        }
        
        return "Idle";
    }
    
    public override void OnExit(IAIMemory memory)
    {
        // 清理待机状态数据
    }
}

// ===== 2. 巡逻状态 =====
public class PatrolState : FSMState
{
    private Vector3[] _patrolPoints;
    private int _currentPoint = 0;
    
    public PatrolState(Vector3[] patrolPoints)
    {
        StateName = "Patrol";
        _patrolPoints = patrolPoints;
    }
    
    public override void OnEnter(IAIMemory memory)
    {
        _currentPoint = 0;
        // 开始移动到第一个巡逻点
    }
    
    public override string OnUpdate(IAIMemory memory, out string nextState)
    {
        nextState = null;
        
        // 移动到当前巡逻点
        MoveTo(_patrolPoints[_currentPoint]);
        
        // 到达后切换到下一个点
        if (ReachedTarget())
        {
            _currentPoint = (_currentPoint + 1) % _patrolPoints.Length;
        }
        
        // 发现敌人则切换到攻击
        if (memory.Get<bool>("EnemySpotted"))
        {
            nextState = "Attack";
        }
        
        return "Patrol";
    }
}

// ===== 3. 攻击状态 =====
public class AttackState : FSMState
{
    private float _attackCooldown = 0f;
    private const float ATTACK_INTERVAL = 1.5f;
    
    public AttackState() => StateName = "Attack";
    
    public override void OnEnter(IAIMemory memory)
    {
        _attackCooldown = 0f;
        // 播放攻击准备动画
    }
    
    public override string OnUpdate(IAIMemory memory, out string nextState)
    {
        nextState = null;
        _attackCooldown += Time.deltaTime;
        
        // 敌人消失则切换回巡逻
        if (!memory.Get<bool>("EnemySpotted"))
        {
            nextState = "Patrol";
            return "Idle";
        }
        
        // 冷却完成后执行攻击
        if (_attackCooldown >= ATTACK_INTERVAL)
        {
            _attackCooldown = 0f;
            PerformAttack();
        }
        
        return "Attack";
    }
}
```

### 注意事项

- StateName 必须在构造函数中赋值，用于状态注册和跳转
- nextState 参数为 null 表示不跳转，赋值表示请求跳转
- OnUpdate 返回的字符串是行动意图，由 Actor 执行
- 避免在 OnEnter/OnExit 中执行耗时操作
- 状态跳转在当前帧 OnUpdate 结束后执行

## 5. FSMTinker 思考者

基于有限状态机 (FSM) 的思考者实现，管理状态注册、跳转和决策输出。

### 状态机流程

```
┌─────────────────────────────────────────────────────────┐
│                    FSMTinker.Think()                    │
│                                                         │
│  1. 首次运行？ → 是 → 进入初始状态 (OnEnter)            │
│       ↓ 否                                              │
│  2. 执行当前状态 OnUpdate() → 获取意图和 nextState      │
│       ↓                                                 │
│  3. nextState 有值？ → 是 → 状态跳转 (OnExit→OnEnter)   │
│       ↓ 否                                              │
│  4. 返回意图给 Actor 执行                               │
└─────────────────────────────────────────────────────────┘
```

### 使用示例

```csharp
// ===== 1. 配置 FSM =====
var tinker = new FSMTinker();

// 注册状态
tinker.AddState(new IdleState());
tinker.AddState(new PatrolState(patrolPoints));
tinker.AddState(new AttackState());
tinker.AddState(new ChaseState());
tinker.AddState(new FleeState());

// 设置初始状态
tinker.SetInitialState("Idle");

// 在 AIActor.SetupAI 中返回
// return (memory, tinker, observers);

// ===== 2. 状态跳转示例 =====
// 在 FSMState.OnUpdate 中请求跳转
public override string OnUpdate(IAIMemory memory, out string nextState)
{
    nextState = null; // 默认不跳转
    
    // 条件满足时请求跳转
    if (memory.Get<bool>("EnemySpotted"))
    {
        nextState = "Attack"; // 请求跳转到攻击状态
    }
    else if (memory.Get<float>("Health") < 30)
    {
        nextState = "Flee"; // 请求跳转到逃跑状态
    }
    
    return "Patrol"; // 返回当前行动意图
}

// ===== 3. 完整 AI 装配示例 =====
public class GuardActor : AIActor
{
    [SerializeField] private Vector3[] patrolPoints;
    
    protected override void SetupAI(out IAIMemory memory, out IAITinker tinker, out List<IAIObserver> observers)
    {
        // 1. 创建记忆
        memory = new KVMemory();
        
        // 2. 创建 FSM 思考者
        var fsm = new FSMTinker();
        fsm.AddState(new IdleState());
        fsm.AddState(new PatrolState(patrolPoints));
        fsm.AddState(new AttackState());
        fsm.AddState(new ChaseState());
        fsm.SetInitialState("Idle");
        tinker = fsm;
        
        // 3. 创建感知器
        observers = new List<IAIObserver>
        {
            new VisionObserver(viewRange: 15f),
            new HearingObserver(hearRange: 10f)
        };
    }
    
    protected override void Act(string intendedAction)
    {
        // 根据意图执行具体行为
        Debug.Log($"执行动作：{intendedAction}");
    }
}
```

### 注意事项

- 必须设置初始状态，否则 Think 返回空字符串
- 状态名称必须唯一，后注册的同名状态会覆盖之前的
- 状态跳转在当前帧 OnUpdate 结束后执行
- ChangeState 是私有方法，外部通过 nextState 参数请求跳转
- Init 方法为空，状态进入延迟到第一次 Think 时执行

## 完整示例

### 守卫 AI

```csharp
// ===== 1. 定义记忆键名常量 =====
public static class MemoryKeys
{
    public const string EnemySpotted = "EnemySpotted";
    public const string EnemyDistance = "EnemyDistance";
    public const string EnemyPosition = "EnemyPosition";
    public const string Health = "Health";
    public const string IsAlert = "IsAlert";
}

// ===== 2. 定义感知器 =====
public class VisionObserver : IAIObserver
{
    private float _viewRange;
    private LayerMask _targetLayer;
    
    public VisionObserver(float viewRange, LayerMask targetLayer)
    {
        _viewRange = viewRange;
        _targetLayer = targetLayer;
    }
    
    public void Init() { }
    public void UnInit() { }
    
    public void Observe(IAIMemory memory)
    {
        // 球形检测视野内的目标
        var colliders = Physics.OverlapSphere(transform.position, _viewRange, _targetLayer);
        
        if (colliders.Length > 0)
        {
            var target = colliders[0].transform;
            memory.Set(MemoryKeys.EnemySpotted, true);
            memory.Set(MemoryKeys.EnemyPosition, target.position);
            memory.Set(MemoryKeys.EnemyDistance, Vector3.Distance(transform.position, target.position));
        }
        else
        {
            memory.Set(MemoryKeys.EnemySpotted, false);
        }
    }
}

// ===== 3. 定义 FSM 状态 =====
public class GuardIdleState : FSMState
{
    private float _timer = 0f;
    
    public GuardIdleState() => StateName = "Idle";
    
    public override void OnEnter(IAIMemory memory)
    {
        _timer = 0f;
        memory.Set(MemoryKeys.IsAlert, false);
    }
    
    public override string OnUpdate(IAIMemory memory, out string nextState)
    {
        nextState = null;
        _timer += Time.deltaTime;
        
        if (memory.Get<bool>(MemoryKeys.EnemySpotted))
        {
            nextState = "Attack";
            return "Alert";
        }
        
        if (_timer >= 3f)
        {
            nextState = "Patrol";
        }
        
        return "Idle";
    }
}

public class GuardPatrolState : FSMState
{
    private Vector3[] _points;
    private int _index = 0;
    
    public GuardPatrolState(Vector3[] points)
    {
        StateName = "Patrol";
        _points = points;
    }
    
    public override void OnEnter(IAIMemory memory)
    {
        _index = 0;
    }
    
    public override string OnUpdate(IAIMemory memory, out string nextState)
    {
        nextState = null;
        
        if (memory.Get<bool>(MemoryKeys.EnemySpotted))
        {
            nextState = "Attack";
            return "Alert";
        }
        
        // 移动到巡逻点
        var targetPos = _points[_index];
        if (Vector3.Distance(transform.position, targetPos) < 0.5f)
        {
            _index = (_index + 1) % _points.Length;
        }
        
        return "Patrol";
    }
}

public class GuardAttackState : FSMState
{
    private float _cooldown = 0f;
    
    public GuardAttackState() => StateName = "Attack";
    
    public override void OnEnter(IAIMemory memory)
    {
        _cooldown = 0f;
        memory.Set(MemoryKeys.IsAlert, true);
    }
    
    public override string OnUpdate(IAIMemory memory, out string nextState)
    {
        nextState = null;
        _cooldown += Time.deltaTime;
        
        if (!memory.Get<bool>(MemoryKeys.EnemySpotted))
        {
            nextState = "Patrol";
            return "Search";
        }
        
        if (_cooldown >= 1.5f)
        {
            _cooldown = 0f;
            return "Attack";
        }
        
        return "Chase";
    }
}

// ===== 4. 定义 AIActor =====
public class GuardActor : AIActor
{
    [Header("巡逻配置")]
    [SerializeField] private Vector3[] patrolPoints;
    [SerializeField] private float viewRange = 15f;
    [SerializeField] private LayerMask targetLayer;
    
    protected override void SetupAI(out IAIMemory memory, out IAITinker tinker, out List<IAIObserver> observers)
    {
        memory = new KVMemory();
        
        var fsm = new FSMTinker();
        fsm.AddState(new GuardIdleState());
        fsm.AddState(new GuardPatrolState(patrolPoints));
        fsm.AddState(new GuardAttackState());
        fsm.SetInitialState("Idle");
        tinker = fsm;
        
        observers = new List<IAIObserver>
        {
            new VisionObserver(viewRange, targetLayer)
        };
    }
    
    protected override void Act(string intendedAction)
    {
        switch (intendedAction)
        {
            case "Idle":
                // 播放待机动画
                break;
            case "Patrol":
                // 执行巡逻移动
                break;
            case "Alert":
                // 播放警戒动画
                break;
            case "Chase":
                // 追逐敌人
                break;
            case "Attack":
                // 执行攻击
                break;
            case "Search":
                // 搜索敌人
                break;
        }
    }
}
```

## 通用注意事项

### 初始化

| 模块 | 是否需要手动初始化 |
|---|---|
| AIActor | ❌ 自动初始化（Start） |
| IAIMemory | ❌ 由 AIActor 级联初始化 |
| IAITinker | ❌ 由 AIActor 级联初始化 |
| IAIObserver | ❌ 由 AIActor 级联初始化 |
| KVMemory | ❌ 由 AIActor 级联初始化 |
| FSMTinker | ❌ 由 AIActor 级联初始化 |

### 最佳实践

1. 组件命名：使用 功能 + Observer/Tinker/State 格式，如 VisionObserver、AttackState
2. 记忆键名：使用常量类管理，避免硬编码字符串
3. 状态设计：每个状态职责单一，避免状态过于复杂
4. 感知优化：使用物理层掩码、距离检测等优化感知性能
5. 内存管理：在 OnExit 中清理状态数据，避免内存泄漏
6. 调试支持：在状态切换时输出日志，便于调试 AI 行为

### 常见问题

| 问题 | 解决方案 |
|---|---|
| AI 不执行动作 | 检查 SetupAI 是否正确装配组件 |
| 状态不跳转 | 检查 nextState 是否正确赋值 |
| 记忆数据丢失 | 检查是否意外调用了 Init/UnInit |
| 性能问题 | 优化 Observer 的检测逻辑，使用层掩码 |
| 状态卡死 | 确保每个状态都有退出条件 |
| Act 不执行 | 检查 Think 是否返回了非空意图 |

### 扩展方向

1. 行为树：实现 IAITinker 接口，替换 FSM
2. GOAP：实现目标导向的行动计划系统
3. 效用 AI：基于效用分数的决策系统
4. 黑板模式：实现更复杂的 IAIMemory
5. 感知系统：扩展更多 Observer 类型（听觉、触觉、嗅觉等）
