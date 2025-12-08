这是一个 **GoveKits.Unit 框架终极开发手册**。

这套文档将从架构设计理念、核心底层、业务模块详解、到完整的实战工作流，进行“原子级”的详细拆解。即便你明天失忆了，拿着这份文档也能把系统重新搭起来。

---

# GoveKits.Unit 开发者手册 (v1.0)

## 📚 目录

1.  **架构总览 (Architecture Overview)**
2.  **基石：GameTag 与 TagQuery**
3.  **神经系统：EventBus 事件总线**
4.  **骨架：Unit 结构与生命周期**
5.  **数值心脏：Attribute 属性系统**
6.  **状态皮肤：Mark 标记系统**
7.  **手足：Ability 能力系统**
8.  **反射神经：Reaction 反应系统**
9.  **血液：GameEffect 与 交互流程**
10. **实战演练：从零创建一个战斗单位**
11. **调试与性能优化**

---

## 1. 架构总览 (Architecture Overview)

**GoveKits.Unit** 是一个专为高性能 RPG/ACT/MOBA 游戏设计的**数据驱动 (Data-Driven)**、**零 GC (Zero-Allocation)**、**高度解耦**的单位架构系统。

### 核心哲学
*   **一切皆事件 (Everything is an Event)**：攻击、治疗、死亡、升级，全部通过事件总线驱动，而非直接函数调用。
*   **组合优于继承 (Composition over Inheritance)**：单位的功能由 Attributes、Marks、Abilities、Reactions 四个容器组合而成，而非庞大的基类。
*   **高性能 (High Performance)**：大量使用 Struct 封装、对象池 (`EventPool`)、预计算哈希 (`GameTag`)，杜绝运行时的装箱拆箱和内存分配。

---

## 2. 基石：GameTag 与 TagQuery

在整个框架中，我们**不仅用字符串，也不仅用枚举**，而是使用了 `GameTag`。

### 2.1 GameTag：性能怪兽
`GameTag` 是一个只读 Struct，它是字典查找的核心 Key。

*   **原理**：在构造时计算 `string` 的 `HashCode` 并缓存。
*   **优势**：在 `Dictionary<GameTag, T>` 中查找时，直接对比 Int 值，性能等同于 `int` Key，但调试时又能看到字符串名字。

**使用规范：**
```csharp
// 1. 隐式转换 (推荐日常使用)
GameTag tag = "Fire"; 

// 2. 静态定义 (推荐用于高频系统 Tag)
public static class Tags {
    public static readonly GameTag HP = "HP";
    public static readonly GameTag Stunned = "Stunned";
}

// 3. 比较
if (tag == "Fire") { ... } // 极快
```

### 2.2 TagQuery：逻辑表达式
用于检查单位是否满足特定条件（如：释放技能前检查是否被沉默）。

**支持的语法糖：**
*   `HasTag("A")`：有 A 标签。
*   `!query`：逻辑非。
*   `q1 & q2`：逻辑与 (And)。
*   `q1 | q2`：逻辑或 (Or)。

**示例：**
```csharp
// 定义查询：即使晕眩 OR (沉默 AND 缴械)
TagQuery cannotCast = "Stunned" | ("Silenced" & "Disarmed");

// 匹配
if (unit.Marks.MatchQuery(cannotCast)) {
    Debug.Log("无法施法！");
}
```

---

## 3. 神经系统：EventBus 事件总线

这是系统中最复杂的底层，负责模块间的解耦通讯。

### 3.1 零 GC 设计 (Zero Allocation)
*   **发布**：使用 `EventPool<T>.Get()` 从池中获取事件对象。
*   **回收**：使用 `try...finally { EventPool<T>.Return(evt); }` 强制回收。
*   **遍历**：`EventChannel` 内部使用 `for` 循环而非 `foreach`，避免迭代器 GC。

### 3.2 优先级机制 (EventPriority)
事件监听器有优先级，决定执行顺序。这对于战斗逻辑至关重要。
*   **Highest (-1000)**: 无敌判定、伤害完全抵消。
*   **High (-500)**: 护盾扣除。
*   **Normal (0)**: 实际扣血、属性变更。
*   **Monitor (10000)**: UI 显示、飘字（此时数据已不可修改）。

### 3.3 如何发布事件
**正确姿势（使用对象池）：**
```csharp
// 假设有一个 DamageEvent
public class DamageEvent : EventInfo, new() {
    public float Amount;
    public override void Reset() => Amount = 0;
}

// 发布
EventManager.Publish<DamageEvent>(evt => {
    evt.Amount = 100f; // 在回调中初始化数据
});
// 离开 Publish 方法后，evt 会自动回收到池中，千万不要在外部持有 evt 的引用！
```

### 3.4 如何订阅事件
```csharp
// 方式一：Lambda (返回一个 Action 用于取消订阅)
var unsubscribe = EventManager.Subscribe<DamageEvent>(evt => {
    Debug.Log($"收到伤害: {evt.Amount}");
}, priority: EventPriority.Monitor);

// ... 不需要时 ...
unsubscribe();

// 方式二：类方法 (推荐用于长期存在的对象)
// 需要实现 EventListener<T> 或者手动管理 DelegateListener
```

---

## 4. 骨架：Unit 结构与生命周期

`GameUnit` (或 `UnitBehaviour`) 是所有逻辑的载体。它不包含具体的游戏业务逻辑（如攻击力怎么算），它只包含**四个容器**。

### 4.1 四大容器职责
1.  **Attributes**：存数值（HP, MP, 攻击力）。
2.  **Marks**：存状态（Buff, Debuff, 装备, 标签）。
3.  **Abilities**：存技能（普通攻击, 火球术）。
4.  **Reactions**：存被动反应（受到伤害反击, 死亡掉落）。

### 4.2 生命周期 (Unity MonoBehaviour)
如果你使用 `UnitBehaviour`：
*   **Start()**: 初始化四个容器。
*   **Update()**: 将 `Time.deltaTime` 转发给容器（主要是 `Marks` 需要时间流逝）。
*   **OnDestroy()**: 调用 `Clear()`，断开所有事件监听，防止内存泄漏。

---

## 5. 数值心脏：Attribute 属性系统

这是 RPG 的核心。框架区分了“状态值”和“运行时值”。

### 5.1 StateAttribute (计算型属性)
用于 **MaxHP, Attack, Defense, MoveSpeed**。
*   **公式**：`Value = (Base + Flat) * (1 + PercentAdd) * PercentMult`。
*   **特点**：懒加载计算（Dirty Flag 模式），修改器变动时自动重算。
*   **Override**：支持强制覆盖（如“无敌”状态强制防御力为 99999）。

### 5.2 RuntimeAttribute (资源型属性)
用于 **CurrentHP, CurrentMP, Stamina**。
*   **特点**：依赖于一个 StateAttribute (上限)。
*   **响应式**：如果 MaxHP 变小，CurrentHP 会自动被截断。如果 MaxHP 变大，CurrentHP 保持不变（除非手动处理）。

### 5.3 属性链接 (AttributeLinker)
如何实现“每 1 点力量增加 10 点生命值”？
```csharp
// 建立链接
AttributeLinker.Link(
    source: unit.Attributes["Strength"], 
    target: unit.Attributes["MaxHP"], 
    convertFunc: strength => strength * 10f
);
```
当 Strength 变化时，会自动给 MaxHP 添加/更新一个 Flat 类型的 Modifier。

---

## 6. 状态皮肤：Mark 标记系统

Mark 是 Buff，也是 Debuff，也可以是单纯的 Tag（如 "Undead"）。

### 6.1 Mark 的特性
*   **生命周期**：`OnApply` (获得时), `OnTick` (每帧), `OnStack` (重复获得), `OnRemove` (移除时)。
*   **堆叠逻辑**：
    *   **Duration**：重复获得时，取两者最大时长（刷新机制）。
    *   **Stack**：重复获得时，层数相加（直至 MaxStack）。
*   **永久性**：`Duration = GameMark.Infinite (-1)`。

### 6.2 实现一个 "中毒" Mark
```csharp
public class PoisonMark : GameMark
{
    private float _damagePerSec;
    private float _timer;

    public PoisonMark(float damage, float duration) : base("Poison", duration)
    {
        _damagePerSec = damage;
    }

    public override void OnTick(float dt)
    {
        base.OnTick(dt); // 处理持续时间减少
        
        _timer += dt;
        if (_timer >= 1.0f) {
            _timer = 0;
            // 每秒扣血：向 Owner 发布伤害事件
            // 注意：不要直接改属性，走标准交互流程
            ApplyDamage(); 
        }
    }
}
```

---

## 7. 手足：Ability 能力系统

Ability 封装了“能做什么”。

### 7.1 核心流程 (`Execute`)
1.  **CanExecute (同步)**：检查 Cooldown (专属 CD + GCD)、检查 Cost (MP不足)、检查限制 (被晕眩)。
2.  **Cost.Pay**：扣除资源。
3.  **CommitCooldown**：给自己施加 `CooldownMark`。
4.  **OnExecute (异步)**：执行真正的逻辑（播放动作 -> 等待打击点 -> 判定伤害）。

### 7.2 异步的威力
使用 `UniTask` 可以写出非常线性的技能逻辑：
```csharp
protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
{
    // 1. 播放动画
    animator.Play("CastFireball");
    
    // 2. 等待 0.5秒 前摇
    await UniTask.Delay(500);
    
    // 3. 发射投射物 (伪代码)
    SpawnProjectile(source, target);
    
    // 4. 等待 1秒 后摇
    await UniTask.Delay(1000);
}
```

---

## 8. 反射神经：Reaction 反应系统

Reaction 是单位“感知”世界的方式。如果没有 Reaction，单位就是木桩。

### 8.1 什么是 Reaction？
它是一个**包装好的事件监听器**。
它自动过滤：`if (evt.Target != Me && evt.Source != Me) return;`。
只有与我相关的事件，才会触发我的 Reaction。

### 8.2 系统级 Reaction
每个单位初始化时，通常会自带一个核心 Reaction：**HandleEffect**。
*   监听：`GameEffect` (任何对该单位造成的影响)。
*   行为：解析 Effect 中的 `InstantChanges`（扣血），解析 `ApplyMarks`（加 Buff）。
*   **如果没有这个 Reaction，你打单位一拳，单位也不会扣血！**

### 8.3 自定义 Reaction (荆棘甲示例)
```csharp
var thorns = new GameReaction<GameEffect>("Thorns", unit, effect => {
    // 只有当我是受害者，且是物理攻击时
    if (effect.Target == unit && effect.HasTag("PhysicalDamage")) {
        // 反弹 10 点伤害给来源
        var reflect = EventPool<GameEffect>.Get();
        reflect.Source = unit;
        reflect.Target = effect.Source; // 目标是打我的人
        reflect.AddInstantChange("HP", -10);
        
        EventManager.PublishInternal(reflect); // 发布反伤
    }
});
unit.Reactions.Add("Thorns", thorns);
```

---

## 9. 血液：GameEffect 与 交互流程

`GameEffect` 是所有交互的数据载体。

### 9.1 数据结构
*   **Source / Target**：谁对谁。
*   **InstantChanges**：字典 `Dictionary<GameTag, float>`。如 `{ "HP": -100, "MP": -20 }`。
*   **ApplyMarks**：列表 `List<GameMark>`。如 `{ StunMark }`。
*   **Context Tags**：标签集合。如 `{ "Critical", "Fire", "Melee" }`。

### 9.2 完整伤害流程 (The Loop)
这是一个标准的攻击流程，请仔细阅读：

1.  **发起**：`Player` 释放技能，创建一个 `GameEffect`。
    *   Target: Monster
    *   InstantChanges: "HP" = -100
    *   Tags: "Physical"

2.  **发布**：`EventManager.Publish(effect)`。

3.  **拦截 (High Priority Reactions)**：
    *   Monster 身上有个“护盾” Buff 注册了 Reaction。
    *   Reaction 收到事件，发现 Target 是 Monster。
    *   Reaction 修改 effect.InstantChanges["HP"]，从 -100 改为 -50（护盾抵消）。

4.  **应用 (Normal Priority Reactions)**：
    *   Monster 的系统级 Reaction `HandleEffect` 收到事件。
    *   读取 InstantChanges["HP"] (-50)。
    *   调用 `Monster.Attributes.ApplyRuntimeChange("HP", -50)`。 -> **血量真正减少**。

5.  **反馈 (Low Priority Reactions)**：
    *   Monster 身上有个“受伤触发暴怒” Buff。
    *   Reaction 收到事件，给自己添加一个 "Enrage" Buff。

6.  **显示 (Monitor Priority)**：
    *   UI 系统监听到事件，在 Monster 头顶飘字 "-50"。

---

## 10. 实战演练：从零创建一个战斗单位

假设我们要创建一个简单的 "战士"。

### Step 1: 挂载脚本
在 GameObject 上挂载 `UnitBehaviour` 的子类（如 `HeroUnit`）。

### Step 2: 初始化属性 (在 Start 或 InitializeAttributes 中)
```csharp
public override void InitializeAttributes()
{
    base.InitializeAttributes(); // 创建容器
    
    // 1. 定义上限
    var maxHp = Attributes.AddState("MaxHP", 1000);
    var atk = Attributes.AddState("Attack", 50);
    var def = Attributes.AddState("Defense", 10);
    
    // 2. 定义资源 (依赖上限)
    Attributes.AddRuntime("HP", "MaxHP");
}
```

### Step 3: 定义系统反应 (处理受伤)
确保 `InitializeReactions` 中注册了处理数值变化的 Reaction。
*(框架中的 `GameUnit` 代码建议加上这个默认实现，如下)*:
```csharp
public override void InitializeReactions()
{
    base.InitializeReactions();
    
    // 注册通用效果处理器
    var handler = new GameReaction<GameEffect>("System.HandleEffect", this, e => {
        // 应用数值变化
        if (e.InstantChanges != null) {
            foreach(var kv in e.InstantChanges) 
                Attributes.ApplyRuntimeChange(kv.Key, kv.Value);
        }
        // 应用 Mark
        if (e.ApplyMarks != null) {
            foreach(var m in e.ApplyMarks) 
                Marks.Add(m.Tag, m);
        }
    }, EventPriority.Normal);
    
    Reactions.Add("System.Base", handler);
}
```

### Step 4: 创建技能 "重击"
```csharp
public class HeavyStrike : GameAbility
{
    public HeavyStrike() : base("HeavyStrike") 
    {
        SetCooldown(5.0f); // 5秒冷却
        AddCost("MP", 20); // 消耗20MP
    }

    protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
    {
        Debug.Log("蓄力中...");
        await UniTask.Delay(500); // 0.5s 前摇
        
        // 计算伤害
        float atk = source.Attributes.GetValue("Attack");
        float dmg = atk * 2.0f; // 200% 倍率
        
        // 构建效果
        EventManager.Publish<GameEffect>(e => {
            e.Source = source;
            e.Target = target;
            e.AddInstantChange("HP", -dmg);
            e.AddTag("Physical");
            e.AddTag("Melee");
        });
        
        Debug.Log("重击命中！");
    }
}
```

### Step 5: 安装技能
```csharp
unit.Abilities.Add("Skill.HeavyStrike", new HeavyStrike());
```

### Step 6: 触发
在 PlayerController 中检测按键：
```csharp
if (Input.GetKeyDown(KeyCode.Space)) {
    unit.Abilities.TryUseAbility("Skill.HeavyStrike", targetUnit);
}
```

---

## 11. 调试与性能优化

### 调试神器：UnitDebugBehaviour
框架自带的 `UnitDebugBehaviour` 是必用的。
*   把这个脚本挂在 Unit 上。
*   运行时，屏幕左侧会出现该 Unit 的所有数据：
    *   实时 HP/MaxHP。
    *   身上的 Buff 列表和剩余时间。
    *   技能列表和冷却状态。
*   支持 **Filter**：在输入框输入 "HP"，只显示 HP 相关属性。

### 性能陷阱与规避
1.  **Tag 滥用**：不要在 Update 中 `new GameTag("String")`。请把 Tag 缓存为 `static readonly` 字段。
2.  **Reaction 爆炸**：
    *   **现象**：A 反伤给 B，B 又反伤给 A，无限循环。
    *   **解决**：在 Reaction 逻辑中检查 Tag。`if (effect.HasTag("Reflected")) return;`。反伤发出的 Effect 必须带上 "Reflected" 标签。
3.  **Marks 数量**：`MarkContainer` 会在 Update 遍历所有 Mark。如果单位身上有 100 个 Mark，性能会下降。
    *   **优化**：对于无需 Tick（纯属性加成）的 Mark，可以修改 `GameMark` 代码，增加一个 `bool NeedsTick` 字段，容器遍历时跳过它们。

---

## 12. 高级技巧：Modifier 的移除

这是新手最容易卡住的地方：**我加了一个 Buff 增加了 100 攻击力，Buff 消失时怎么把这 100 扣掉？**

**GoveKits.Unit 是全自动的！**

1.  **Mark (Buff) 的实现**：
    ```csharp
    public class RageBuff : GameMark 
    {
        public RageBuff() : base("Rage", 10.0f) {} // 10秒

        public override void OnApply(IGameUnit owner, IGameUnit source)
        {
            base.OnApply(owner, source);
            // 给 Attack 属性添加一个 Modifier，Source 设为 "this" (这个 Buff 实例)
            var mod = new GameModifier(ModifierType.PercentAdd, 0.5f, this);
            owner.Attributes.ApplyStateModifier("Attack", mod);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            // 框架不知道你要移除哪个 Modifier，但它知道 Source
            // 告诉属性容器：把所有 Source 是 "this" 的 Modifier 删掉
            Owner.Attributes.RemoveModifiersFromAllAttributes(this);
        }
    }
    ```

**重点**：`GameModifier` 的第三个参数 `source` 是关键。只要在 Add 时传入了 source对象，Remove 时就能根据对象引用精准删除。

---

## 结语

**GoveKits.Unit** 提供的是积木。
*   `GameTag` 是卡扣。
*   `EventBus` 是电流。
*   `Attribute/Mark` 是积木块。

不要去修改 `EventBus.cs` 或 `AttributeContainer.cs` 的核心代码，而是通过 **扩展 Reaction** 和 **自定义 Ability/Mark** 来实现你的游戏玩法。这才是数据驱动开发的真谛。

Keep it Zero-Alloc, Keep it Decoupled. 祝开发愉快！