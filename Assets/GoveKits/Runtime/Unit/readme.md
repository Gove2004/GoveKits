这份文档是 **GoveKits.Unit** 的终极架构白皮书与开发手册。
这份文档是 **GoveKits.Unit** 的终极架构白皮书与开发手册。

本文档深入剖析了框架的设计哲学、底层实现细节、各模块的协同工作流以及在实际工业级项目中的最佳实践。它不仅是一份说明书，更是一份关于**高性能 Unity 架构**的教案。
本文档深入剖析了框架的设计哲学、底层实现细节、各模块的协同工作流以及在实际工业级项目中的最佳实践。它不仅是一份说明书，更是一份关于**高性能 Unity 架构**的教案。

---

# ⚔️ GoveKits.Unit 架构白皮书与开发手册

> **版本 (Version):** 2.0.0 (Enterprise Edition)  
> **适用领域 (Scope):** 中大型 RPG / ACT / MOBA / 策略类游戏的**战斗核心**与**单位状态管理**。  
> **核心特性 (Features):** 零 GC、数据驱动、事件溯源、反应式编程、可视化调试。

---

## 📖 目录索引

### 第一章：设计哲学与架构总览
1.  [核心设计理念](#1-核心设计理念-philosophy)
2.  [架构全景图](#2-架构全景图-architecture)
3.  [为什么选择 GoveKits.Unit？](#3-为什么选择-govekitsunit)

### 第二章：核心底层基础设施
1.  [GameTag：性能与语义的完美统一](#4-gametag-性能与语义的完美统一)
2.  [EventBus：零分配的高速消息总线](#5-eventbus-零分配的高速消息总线)
3.  [UniTask：异步逻辑的基石](#6-unitask-异步逻辑的基石)

### 第三章：单位解剖学 (The Unit Anatomy)
1.  [AttributeContainer：数值心脏与依赖计算](#7-attributecontainer-数值心脏与依赖计算)
2.  [MarkContainer：状态、Buff与时间管理](#8-markcontainer-状态buff与时间管理)
3.  [AbilityContainer：主动行为与资源调度](#9-abilitycontainer-主动行为与资源调度)
4.  [ReactionContainer：被动逻辑与事件响应](#10-reactioncontainer-被动逻辑与事件响应)

### 第四章：反应器模式详解 (The Reactor Pattern)
1.  [模板方法模式的应用](#11-模板方法模式的应用)
2.  [继承模式 vs 委托模式](#12-继承模式-vs-委托模式)
3.  [生命周期与过滤机制](#13-生命周期与过滤机制)

### 第五章：交互与数据流 (Interaction Flow)
1.  [从输入到反馈的完整闭环](#14-从输入到反馈的完整闭环)
2.  [优先级机制 (EventPriority) 详解](#15-优先级机制-eventpriority-详解)

### 第六章：可视化调试系统
1.  [Editor Inspector 深度解析](#16-editor-inspector-深度解析)
2.  [实时数据监控与过滤](#17-实时数据监控与过滤)

### 第七章：最佳实践与规范
1.  [性能优化指南](#18-性能优化指南)
2.  [代码编写规范 (Do's and Don'ts)](#19-代码编写规范-dos-and-donts)
# ⚔️ GoveKits.Unit 架构白皮书与开发手册

> **版本 (Version):** 2.0.0 (Enterprise Edition)  
> **适用领域 (Scope):** 中大型 RPG / ACT / MOBA / 策略类游戏的**战斗核心**与**单位状态管理**。  
> **核心特性 (Features):** 零 GC、数据驱动、事件溯源、反应式编程、可视化调试。

---

## 📖 目录索引

### 第一章：设计哲学与架构总览
1.  [核心设计理念](#1-核心设计理念-philosophy)
2.  [架构全景图](#2-架构全景图-architecture)
3.  [为什么选择 GoveKits.Unit？](#3-为什么选择-govekitsunit)

### 第二章：核心底层基础设施
1.  [GameTag：性能与语义的完美统一](#4-gametag-性能与语义的完美统一)
2.  [EventBus：零分配的高速消息总线](#5-eventbus-零分配的高速消息总线)
3.  [UniTask：异步逻辑的基石](#6-unitask-异步逻辑的基石)

### 第三章：单位解剖学 (The Unit Anatomy)
1.  [AttributeContainer：数值心脏与依赖计算](#7-attributecontainer-数值心脏与依赖计算)
2.  [MarkContainer：状态、Buff与时间管理](#8-markcontainer-状态buff与时间管理)
3.  [AbilityContainer：主动行为与资源调度](#9-abilitycontainer-主动行为与资源调度)
4.  [ReactionContainer：被动逻辑与事件响应](#10-reactioncontainer-被动逻辑与事件响应)

### 第四章：反应器模式详解 (The Reactor Pattern)
1.  [模板方法模式的应用](#11-模板方法模式的应用)
2.  [继承模式 vs 委托模式](#12-继承模式-vs-委托模式)
3.  [生命周期与过滤机制](#13-生命周期与过滤机制)

### 第五章：交互与数据流 (Interaction Flow)
1.  [从输入到反馈的完整闭环](#14-从输入到反馈的完整闭环)
2.  [优先级机制 (EventPriority) 详解](#15-优先级机制-eventpriority-详解)

### 第六章：可视化调试系统
1.  [Editor Inspector 深度解析](#16-editor-inspector-深度解析)
2.  [实时数据监控与过滤](#17-实时数据监控与过滤)

### 第七章：最佳实践与规范
1.  [性能优化指南](#18-性能优化指南)
2.  [代码编写规范 (Do's and Don'ts)](#19-代码编写规范-dos-and-donts)

---

## 第一章：设计哲学与架构总览

### 1. 核心设计理念 (Philosophy)
## 第一章：设计哲学与架构总览

### 1. 核心设计理念 (Philosophy)

GoveKits.Unit 的诞生是为了解决 Unity 游戏开发中常见的“代码屎山”问题：逻辑耦合严重、GC 频繁触发、扩展新技能困难。

1.  **组合优于继承 (Composition over Inheritance)**
    *   单位不再是一个继承树深不见底的 `Character` 类。单位只是一个**容器的载体**。无论是英雄、怪物、防御塔还是可破坏的箱子，本质上都是 `Attribute` + `Mark` + `Reaction` 的组合。
2.  **数据驱动逻辑 (Data-Driven)**
    *   逻辑不应该写在 `Update` 里。逻辑应当是对**数据变化**的响应。血量减少不是因为调用了 `TakeDamage()`，而是因为系统响应了 `CombatEvent` 并应用了数值变更。
3.  **零 GC (Zero-Allocation)**
    *   在战斗这种高频场景下，任何 `new` 操作都是不可接受的。框架通过对象池、结构体哈希、无枚举器遍历等手段，实现了运行时的零垃圾产生。
4.  **反应式编程 (Reactive)**
    *   交互的本质是：**发起者发布意图 -> 中间件拦截/修改 -> 接受者应用结果**。通过 `Reaction` 系统，我们将这一链条完全解耦。
GoveKits.Unit 的诞生是为了解决 Unity 游戏开发中常见的“代码屎山”问题：逻辑耦合严重、GC 频繁触发、扩展新技能困难。

1.  **组合优于继承 (Composition over Inheritance)**
    *   单位不再是一个继承树深不见底的 `Character` 类。单位只是一个**容器的载体**。无论是英雄、怪物、防御塔还是可破坏的箱子，本质上都是 `Attribute` + `Mark` + `Reaction` 的组合。
2.  **数据驱动逻辑 (Data-Driven)**
    *   逻辑不应该写在 `Update` 里。逻辑应当是对**数据变化**的响应。血量减少不是因为调用了 `TakeDamage()`，而是因为系统响应了 `CombatEvent` 并应用了数值变更。
3.  **零 GC (Zero-Allocation)**
    *   在战斗这种高频场景下，任何 `new` 操作都是不可接受的。框架通过对象池、结构体哈希、无枚举器遍历等手段，实现了运行时的零垃圾产生。
4.  **反应式编程 (Reactive)**
    *   交互的本质是：**发起者发布意图 -> 中间件拦截/修改 -> 接受者应用结果**。通过 `Reaction` 系统，我们将这一链条完全解耦。

### 2. 架构全景图 (Architecture)

```mermaid
graph TD
    User[用户输入 / AI决策] --> Ability[Ability Container]
    Ability --1.检查与消耗--> Ability
    Ability --2.发布--> EventBus[Event Bus (Pool)]
    
    EventBus --3.分发(按优先级)--> GlobalReaction[全局系统反应]
    EventBus --3.分发(按优先级)--> UnitReaction[单位 Reaction Container]
    
    UnitReaction --4.读取状态--> Mark[Mark Container]
    UnitReaction --5.修改数值--> Attribute[Attribute Container]
    
    Attribute --6.变更通知--> UI[UI 系统]
    Mark --7.状态变更--> UI
```

### 3. 为什么选择 GoveKits.Unit？
*   **解耦**：写“火球术”的人不需要知道“护盾”是怎么工作的。写“反伤甲”的人不需要知道“攻击者”是谁。
*   **稳健**：通过 `EventInfo` 的强类型约束和生命周期管理，减少了空引用和逻辑冲突。
*   **调试**：内置的 Inspector 可以在运行时清晰地展示单位内部所有数据的快照，极大降低 Debug 成本。
### 2. 架构全景图 (Architecture)

```mermaid
graph TD
    User[用户输入 / AI决策] --> Ability[Ability Container]
    Ability --1.检查与消耗--> Ability
    Ability --2.发布--> EventBus[Event Bus (Pool)]
    
    EventBus --3.分发(按优先级)--> GlobalReaction[全局系统反应]
    EventBus --3.分发(按优先级)--> UnitReaction[单位 Reaction Container]
    
    UnitReaction --4.读取状态--> Mark[Mark Container]
    UnitReaction --5.修改数值--> Attribute[Attribute Container]
    
    Attribute --6.变更通知--> UI[UI 系统]
    Mark --7.状态变更--> UI
```

### 3. 为什么选择 GoveKits.Unit？
*   **解耦**：写“火球术”的人不需要知道“护盾”是怎么工作的。写“反伤甲”的人不需要知道“攻击者”是谁。
*   **稳健**：通过 `EventInfo` 的强类型约束和生命周期管理，减少了空引用和逻辑冲突。
*   **调试**：内置的 Inspector 可以在运行时清晰地展示单位内部所有数据的快照，极大降低 Debug 成本。

---

## 第二章：核心底层基础设施

### 4. GameTag：性能与语义的完美统一
## 第二章：核心底层基础设施

### 4. GameTag：性能与语义的完美统一

在 C# 中，使用 `string` 作为字典的 Key 会导致大量的 `GetHashCode` 计算和字符串比较，且会产生临时的 string 对象。使用 `enum` 又面临扩展困难的问题（DLC 或 Mod 支持差）。
在 C# 中，使用 `string` 作为字典的 Key 会导致大量的 `GetHashCode` 计算和字符串比较，且会产生临时的 string 对象。使用 `enum` 又面临扩展困难的问题（DLC 或 Mod 支持差）。

**GameTag** 是一个极致优化的 `readonly struct`：
**GameTag** 是一个极致优化的 `readonly struct`：

*   **预计算哈希**：在构造时（`new GameTag("HP")`），立即计算并缓存字符串的 `HashCode` 到 `int Id` 字段。
*   **O(1) 查找**：实现 `IEquatable<GameTag>`。在 `Dictionary` 中查找时，直接比较 `Id` (int)，避免了任何字符串操作。
*   **调试友好**：保留原始 `string _name` 仅用于 Inspector 显示和 `ToString()`，不参与逻辑运算。
*   **隐式转换**：代码中可以直接使用字符串赋值，语法糖背后是高效的结构体封装。
*   **预计算哈希**：在构造时（`new GameTag("HP")`），立即计算并缓存字符串的 `HashCode` 到 `int Id` 字段。
*   **O(1) 查找**：实现 `IEquatable<GameTag>`。在 `Dictionary` 中查找时，直接比较 `Id` (int)，避免了任何字符串操作。
*   **调试友好**：保留原始 `string _name` 仅用于 Inspector 显示和 `ToString()`，不参与逻辑运算。
*   **隐式转换**：代码中可以直接使用字符串赋值，语法糖背后是高效的结构体封装。

**最佳实践**：
**最佳实践**：
```csharp
// 推荐：将高频 Tag 缓存为静态只读字段，避免重复构造
// 推荐：将高频 Tag 缓存为静态只读字段，避免重复构造
public static class Tags {
    public static readonly GameTag HP = "HP";
    public static readonly GameTag Stun = "Stun";
    public static readonly GameTag Stun = "Stun";
}
```

### 5. EventBus：零分配的高速消息总线

这是系统的神经中枢。不同于 C# 原生 `event` 或 `UnityEvent`，GoveKits 的 EventBus 专注于**高频、带数据**的广播。

*   **对象池化 (`EventPool<T>`)**：
    *   所有事件类继承自 `EventInfo` 并实现 `Reset()`。
    *   `Publish` 时自动从池中 `Dequeue`，分发完毕后自动 `Enqueue`。
    *   **结果**：无论每秒发生多少次战斗交互，堆内存分配恒定，GC 为 0。
*   **无枚举器遍历**：
    *   `EventChannel` 内部维护 `List<EventListener>`。
    *   分发时使用 `for` 循环倒序遍历，避免了 `foreach` 产生的 `List.Enumerator` 垃圾，同时允许在回调中安全地 `Unsubscribe` 自身。
*   **强类型频道**：
    *   基于 `Type` 的字典索引，确保消息只发送给订阅了该特定类型的监听器，无多余的类型转换开销。
### 5. EventBus：零分配的高速消息总线

这是系统的神经中枢。不同于 C# 原生 `event` 或 `UnityEvent`，GoveKits 的 EventBus 专注于**高频、带数据**的广播。

*   **对象池化 (`EventPool<T>`)**：
    *   所有事件类继承自 `EventInfo` 并实现 `Reset()`。
    *   `Publish` 时自动从池中 `Dequeue`，分发完毕后自动 `Enqueue`。
    *   **结果**：无论每秒发生多少次战斗交互，堆内存分配恒定，GC 为 0。
*   **无枚举器遍历**：
    *   `EventChannel` 内部维护 `List<EventListener>`。
    *   分发时使用 `for` 循环倒序遍历，避免了 `foreach` 产生的 `List.Enumerator` 垃圾，同时允许在回调中安全地 `Unsubscribe` 自身。
*   **强类型频道**：
    *   基于 `Type` 的字典索引，确保消息只发送给订阅了该特定类型的监听器，无多余的类型转换开销。

---

## 第三章：单位解剖学 (The Unit Anatomy)

`UnitBehaviour` 是单位的物理载体，它通过持有四个容器来赋予单位生命。
## 第三章：单位解剖学 (The Unit Anatomy)

`UnitBehaviour` 是单位的物理载体，它通过持有四个容器来赋予单位生命。

### 7. AttributeContainer：数值心脏与依赖计算

RPG 的数值不仅仅是简单的加减法，它涉及**层级计算**和**依赖关系**。

#### A. StateAttribute (状态属性)
用于描述单位的“面板属性”（如攻击力、防御力、最大生命值）。
**计算公式**：
$$ Value = (Base + \sum Flat) \times (1 + \sum Add\%) \times \prod (1 + Mult\%) $$

*   **脏标记模式 (Dirty Flag)**：添加/移除 `Modifier` 时仅标记为 Dirty。只有在访问 `Value` 时，如果 Dirty 为真，才执行公式计算。这避免了在一帧内多次修改导致多次无用的重算。
*   **Override 支持**：特殊的 `ModifierType.Override` 允许特殊状态（如剧情杀、无敌）直接锁定属性值，跳过公式计算。
### 7. AttributeContainer：数值心脏与依赖计算

RPG 的数值不仅仅是简单的加减法，它涉及**层级计算**和**依赖关系**。

#### A. StateAttribute (状态属性)
用于描述单位的“面板属性”（如攻击力、防御力、最大生命值）。
**计算公式**：
$$ Value = (Base + \sum Flat) \times (1 + \sum Add\%) \times \prod (1 + Mult\%) $$

*   **脏标记模式 (Dirty Flag)**：添加/移除 `Modifier` 时仅标记为 Dirty。只有在访问 `Value` 时，如果 Dirty 为真，才执行公式计算。这避免了在一帧内多次修改导致多次无用的重算。
*   **Override 支持**：特殊的 `ModifierType.Override` 允许特殊状态（如剧情杀、无敌）直接锁定属性值，跳过公式计算。

#### B. RuntimeAttribute (运行时属性)
用于描述“当前资源”（如当前血量、当前蓝量）。
*   **响应式依赖**：
    *   创建时绑定一个 `StateAttribute` (Max)。
    *   当 `Max` 变化时（例如穿上装备），`Runtime` 属性会自动响应：
        *   若 `Current > NewMax`，自动截断。
        *   若需实现“百分比保持”或“差值保持”，可在此处扩展。
*   **自动钳制**：任何修改都会保证值在 `[0, Max]` 之间，业务逻辑无需手动 `Mathf.Clamp`。

### 8. MarkContainer：状态、Buff与时间管理

Mark 是单位身上的“贴纸”。它可以是一个 Buff（燃烧），一个 Debuff（眩晕），或者一个纯标签（不死族）。

*   **智能堆叠**：
    *   **时间 (Duration)**：`Mathf.Max(Old, New)`。新状态刷新持续时间。
    *   **层数 (Stack)**：`Mathf.Min(Old + New, MaxStack)`。同类状态叠加层数。
*   **生命周期回调**：
    *   `OnApply`: 初始化逻辑（如添加属性修改器）。
    *   `OnTick`: 每帧逻辑（如 DOT 伤害，仅在 Duration > 0 时触发）。
    *   `OnRemove`: 清理逻辑（务必在此移除属性修改器）。
*   **TagQuery 系统**：
    *   支持 `Marks.MatchQuery("Stun" | ("Root" & "Silence"))` 这样的复杂布尔查询，是技能系统判断能否施法的核心。

### 9. AbilityContainer：主动行为与资源调度

技能是单位与世界交互的主动手段。

*   **TryUseAbility**：
    *   外部（AI/UI）调用的唯一入口。
    *   **原子性操作**：`Check` -> `Pay` -> `Execute`。要么全部成功，要么完全不执行。
*   **UniTask 异步流**：
    *   `Execute` 方法是 `async UniTask`。这意味着你可以用线性的代码编写复杂的时间逻辑：
    *   *播放前摇 -> await 0.3s -> 发射火球 -> await 0.5s -> 播放后摇*。
*   **自动管理**：
    *   **Cooldown**：自动处理技能独立 CD 和公共 CD (GCD)。
    *   **Cost**：自动检查并扣除 AttributeContainer 中的资源。

### 10. ReactionContainer：被动逻辑与事件响应

**这是 v2.0 的灵魂。** 单位不再是被动的数值包，它通过 Reaction 对环境做出反应。

*   **自动激活/休眠**：
    *   单位死亡或冻结时，可以一键 `SetActive(false)`，切断所有事件监听，节省性能。
*   **类型安全**：
    *   泛型约束确保了 Reaction 只能监听特定的 `GameEffect`，消除了类型转换错误。
#### B. RuntimeAttribute (运行时属性)
用于描述“当前资源”（如当前血量、当前蓝量）。
*   **响应式依赖**：
    *   创建时绑定一个 `StateAttribute` (Max)。
    *   当 `Max` 变化时（例如穿上装备），`Runtime` 属性会自动响应：
        *   若 `Current > NewMax`，自动截断。
        *   若需实现“百分比保持”或“差值保持”，可在此处扩展。
*   **自动钳制**：任何修改都会保证值在 `[0, Max]` 之间，业务逻辑无需手动 `Mathf.Clamp`。

### 8. MarkContainer：状态、Buff与时间管理

Mark 是单位身上的“贴纸”。它可以是一个 Buff（燃烧），一个 Debuff（眩晕），或者一个纯标签（不死族）。

*   **智能堆叠**：
    *   **时间 (Duration)**：`Mathf.Max(Old, New)`。新状态刷新持续时间。
    *   **层数 (Stack)**：`Mathf.Min(Old + New, MaxStack)`。同类状态叠加层数。
*   **生命周期回调**：
    *   `OnApply`: 初始化逻辑（如添加属性修改器）。
    *   `OnTick`: 每帧逻辑（如 DOT 伤害，仅在 Duration > 0 时触发）。
    *   `OnRemove`: 清理逻辑（务必在此移除属性修改器）。
*   **TagQuery 系统**：
    *   支持 `Marks.MatchQuery("Stun" | ("Root" & "Silence"))` 这样的复杂布尔查询，是技能系统判断能否施法的核心。

### 9. AbilityContainer：主动行为与资源调度

技能是单位与世界交互的主动手段。

*   **TryUseAbility**：
    *   外部（AI/UI）调用的唯一入口。
    *   **原子性操作**：`Check` -> `Pay` -> `Execute`。要么全部成功，要么完全不执行。
*   **UniTask 异步流**：
    *   `Execute` 方法是 `async UniTask`。这意味着你可以用线性的代码编写复杂的时间逻辑：
    *   *播放前摇 -> await 0.3s -> 发射火球 -> await 0.5s -> 播放后摇*。
*   **自动管理**：
    *   **Cooldown**：自动处理技能独立 CD 和公共 CD (GCD)。
    *   **Cost**：自动检查并扣除 AttributeContainer 中的资源。

### 10. ReactionContainer：被动逻辑与事件响应

**这是 v2.0 的灵魂。** 单位不再是被动的数值包，它通过 Reaction 对环境做出反应。

*   **自动激活/休眠**：
    *   单位死亡或冻结时，可以一键 `SetActive(false)`，切断所有事件监听，节省性能。
*   **类型安全**：
    *   泛型约束确保了 Reaction 只能监听特定的 `GameEffect`，消除了类型转换错误。

---

## 第四章：反应器模式详解 (The Reactor Pattern)

### 11. 模板方法模式的应用

在 v2.0 中，`GameReaction<T>` 被重构为模板方法模式 (Template Method Pattern)。

*   **基类 (`GameReaction`) 负责机制**：
    *   `OnEventReceived` 方法是 `sealed` 或 `private` 的入口。
    *   它负责通用逻辑：**过滤 (Filter)** 和 **异常捕获 (Try-Catch)**。
    *   它检查 `evt.Target == _owner || evt.Source == _owner`，确保只有相关的事件才会被处理。
*   **子类 (`ConcreteReaction`) 负责业务**：
    *   实现 `OnExecute(T evt)` 抽象方法。
    *   开发者只需要关注“收到事件后怎么做”，无需关心订阅、过滤和销毁。

### 12. 继承模式 vs 委托模式

框架提供了两种方式来定义逻辑，适应不同场景：

#### 🔴 继承模式 (Inheritance Mode) - **推荐**
适用于：通用的游戏规则、复杂的被动技能、装备特效。
*   **优点**：代码结构清晰，易于复用，支持成员变量（如累积伤害池）。
*   **示例**：
    ```csharp
    public class DamageReductionReaction : GameReaction<CombatEvent> {
        // ... 实现 OnExecute 计算减伤 ...
    }
    ```

#### 🔵 委托模式 (Delegate Mode) - **便捷**
适用于：快速原型、临时调试、极简单的逻辑（如飘字）。
*   **优点**：无需新建类文件，一行代码搞定。
*   **实现**：通过 `DelegateReaction<T>` 适配器类实现。
*   **示例**：
    ```csharp
    Reactions.Add("Log", new DelegateReaction<CombatEvent>("Log", this, e => LogManager.Log(e)));
    ```

### 13. 生命周期与过滤机制

Reaction 的生命周期严格绑定于 Unit。
*   **Create**: `new Reaction(...)`。
*   **Activate**: 订阅 EventBus。
*   **Execute**:
    1.  EventBus 分发。
    2.  基类过滤：`(Target == Me) || (Source == Me)` ?
    3.  通过 -> 调用 `OnExecute`。
*   **Deactivate**: 取消订阅。
*   **Dispose**: Unit 销毁时触发。
## 第四章：反应器模式详解 (The Reactor Pattern)

### 11. 模板方法模式的应用

在 v2.0 中，`GameReaction<T>` 被重构为模板方法模式 (Template Method Pattern)。

*   **基类 (`GameReaction`) 负责机制**：
    *   `OnEventReceived` 方法是 `sealed` 或 `private` 的入口。
    *   它负责通用逻辑：**过滤 (Filter)** 和 **异常捕获 (Try-Catch)**。
    *   它检查 `evt.Target == _owner || evt.Source == _owner`，确保只有相关的事件才会被处理。
*   **子类 (`ConcreteReaction`) 负责业务**：
    *   实现 `OnExecute(T evt)` 抽象方法。
    *   开发者只需要关注“收到事件后怎么做”，无需关心订阅、过滤和销毁。

### 12. 继承模式 vs 委托模式

框架提供了两种方式来定义逻辑，适应不同场景：

#### 🔴 继承模式 (Inheritance Mode) - **推荐**
适用于：通用的游戏规则、复杂的被动技能、装备特效。
*   **优点**：代码结构清晰，易于复用，支持成员变量（如累积伤害池）。
*   **示例**：
    ```csharp
    public class DamageReductionReaction : GameReaction<CombatEvent> {
        // ... 实现 OnExecute 计算减伤 ...
    }
    ```

#### 🔵 委托模式 (Delegate Mode) - **便捷**
适用于：快速原型、临时调试、极简单的逻辑（如飘字）。
*   **优点**：无需新建类文件，一行代码搞定。
*   **实现**：通过 `DelegateReaction<T>` 适配器类实现。
*   **示例**：
    ```csharp
    Reactions.Add("Log", new DelegateReaction<CombatEvent>("Log", this, e => LogManager.Log(e)));
    ```

### 13. 生命周期与过滤机制

Reaction 的生命周期严格绑定于 Unit。
*   **Create**: `new Reaction(...)`。
*   **Activate**: 订阅 EventBus。
*   **Execute**:
    1.  EventBus 分发。
    2.  基类过滤：`(Target == Me) || (Source == Me)` ?
    3.  通过 -> 调用 `OnExecute`。
*   **Deactivate**: 取消订阅。
*   **Dispose**: Unit 销毁时触发。

---

## 第五章：交互与数据流 (Interaction Flow)

### 14. 从输入到反馈的完整闭环

理解这一流程是掌握框架的关键。以下是 **"A 攻击 B"** 的标准生命周期：

1.  **Input**: 玩家按下攻击键。
2.  **Ability Check**: `Hero.Abilities` 检查技能 CD、MP。
3.  **Ability Execute**:
    *   扣除 MP。
    *   生成 `CombatEvent` 对象（从池中）。
    *   填充数据：Source=Hero, Target=Boss, Damage=-100。
    *   `EventManager.Publish(evt)`。
4.  **Event Dispatch (总线分发)**:
    *   **Phase 1: 拦截 (High Priority)**
        *   Boss 的 `InvincibleReaction` 检查无敌状态。若无敌，`evt.Damage = 0`。
    *   **Phase 2: 计算 (Normal Priority)**
        *   Boss 的 `DefenseReaction` 计算减伤。`evt.Damage *= (1-Def%)`。
        *   Hero 的 `CritReaction` 计算暴击。`evt.Damage *= 1.5`。
    *   **Phase 3: 应用 (Normal Priority)**
        *   Boss 的 `TakeDamageReaction` 执行 `Boss.Attributes.ApplyChange("HP", evt.Damage)`。
    *   **Phase 4: 反馈 (Low Priority)**
        *   Hero 的 `LifeStealReaction` 检查造成的伤害，给自己回血。
    *   **Phase 5: 表现 (Monitor Priority)**
        *   UI 管理器读取最终的 `evt.Damage`，在 Boss 头顶生成飘字。
5.  **Event Recycle**: 事件对象被重置并归还池中。

### 15. 优先级机制 (EventPriority) 详解

*   **Instant (-10000)**: 作弊码、系统级强制重置。
*   **Highest (-1000)**: 无敌、伤害完全免疫。
*   **High (-500)**: 护盾（扣除护盾值代替 HP）、格挡。
*   **Normal (0)**: **标准逻辑**（防御计算、暴击计算、实际扣血）。
*   **Low (500)**: 吸血、反伤、击杀触发（因为需要基于最终伤害值）。
*   **Lowest (1000)**: 成就统计、任务进度更新。
*   **Monitor (10000)**: **纯只读**。UI 显示、战斗日志打印。严禁在此阶段修改事件数据。

---

## 第六章：可视化调试系统

### 16. Editor Inspector 深度解析

GoveKits.Unit 提供了名为 `UnitBehaviourEditor` 的 Custom Inspector。在 Unity 编辑器运行时，它接管了 Unit 的面板。

*   **颜色编码**：
    *   🟢 **绿色**：资源充足、技能就绪。
    *   🟡 **黄色**：技能冷却中。
    *   🔴 **红色**：Buff 即将过期、资源不足。
*   **实时监控**：
    *   不需要由程序员打印 Log，设计师可以直接在 Inspector 看到 Attribute 的数值跳动和 Mark 的进度条流逝。

### 17. 实时数据监控与过滤

*   **Filter 搜索框**：
    *   当单位拥有 50 个属性和 20 个状态时，面板会很乱。
    *   在搜索框输入 "Crit"，面板将自动折叠并仅显示包含 "Crit" 的属性、Buff 和反应器。
*   **技能红绿灯**：
    *   技能列表左侧的小圆点指示了技能的可用性。
    *   如果不可用，右侧会显示原因（如 "CD: 2.5s" 或 "Cost"）。

---

## 第七章：最佳实践与规范

### 18. 性能优化指南

1.  **静态 GameTag**：
    *   不要在 Update 中写 `unit.HasTag("Stun")`。这会产生隐式转换开销。
    *   请定义 `static readonly GameTag Stun = "Stun";` 并复用它。
2.  **减少 Mark 的 Tick**：
    *   只有需要每帧执行逻辑（如 DOT）的 Mark 才需要重写 `OnTick`。纯属性加成的 Mark 不需要 Tick，这能显著减少 `Update` 的开销。
3.  **事件池的正确使用**：
    *   永远使用 `EventManager.Publish<T>(initializer)`。
    *   永远不要在回调之外持有 `EventInfo` 的引用，因为该对象会被回收并复用于下一次事件。

### 19. 代码编写规范 (Do's and Don'ts)

*   **✅ Do**: 在 Reaction 中判断 `evt.Target == _owner`。虽然基类过滤了无关事件，但作为 Source（攻击者）和 Target（受击者）收到的事件是一样的，你需要区分你的角色。
*   **✅ Do**: 使用 `AttributeLinker` 处理属性间的依赖（如力量增加血上限）。不要手动写 Update 同步。
*   **⛔ Don't**: 不要直接修改 `Attributes["HP"].Value`。永远通过 `Reaction` 响应 `CombatEvent` 来修改。否则你的无敌盾、减伤甲都将失效。
*   **⛔ Don't**: 不要在 `OnExecute` 中发布同类型的事件且不加终止条件。这会导致无限递归（A 反伤 B，B 反伤 A...）。解决方案是给反伤事件加个 Tag `"Reflected"` 并不再响应它。
## 第五章：交互与数据流 (Interaction Flow)

### 14. 从输入到反馈的完整闭环

理解这一流程是掌握框架的关键。以下是 **"A 攻击 B"** 的标准生命周期：

1.  **Input**: 玩家按下攻击键。
2.  **Ability Check**: `Hero.Abilities` 检查技能 CD、MP。
3.  **Ability Execute**:
    *   扣除 MP。
    *   生成 `CombatEvent` 对象（从池中）。
    *   填充数据：Source=Hero, Target=Boss, Damage=-100。
    *   `EventManager.Publish(evt)`。
4.  **Event Dispatch (总线分发)**:
    *   **Phase 1: 拦截 (High Priority)**
        *   Boss 的 `InvincibleReaction` 检查无敌状态。若无敌，`evt.Damage = 0`。
    *   **Phase 2: 计算 (Normal Priority)**
        *   Boss 的 `DefenseReaction` 计算减伤。`evt.Damage *= (1-Def%)`。
        *   Hero 的 `CritReaction` 计算暴击。`evt.Damage *= 1.5`。
    *   **Phase 3: 应用 (Normal Priority)**
        *   Boss 的 `TakeDamageReaction` 执行 `Boss.Attributes.ApplyChange("HP", evt.Damage)`。
    *   **Phase 4: 反馈 (Low Priority)**
        *   Hero 的 `LifeStealReaction` 检查造成的伤害，给自己回血。
    *   **Phase 5: 表现 (Monitor Priority)**
        *   UI 管理器读取最终的 `evt.Damage`，在 Boss 头顶生成飘字。
5.  **Event Recycle**: 事件对象被重置并归还池中。

### 15. 优先级机制 (EventPriority) 详解

*   **Instant (-10000)**: 作弊码、系统级强制重置。
*   **Highest (-1000)**: 无敌、伤害完全免疫。
*   **High (-500)**: 护盾（扣除护盾值代替 HP）、格挡。
*   **Normal (0)**: **标准逻辑**（防御计算、暴击计算、实际扣血）。
*   **Low (500)**: 吸血、反伤、击杀触发（因为需要基于最终伤害值）。
*   **Lowest (1000)**: 成就统计、任务进度更新。
*   **Monitor (10000)**: **纯只读**。UI 显示、战斗日志打印。严禁在此阶段修改事件数据。

---

## 第六章：可视化调试系统

### 16. Editor Inspector 深度解析

GoveKits.Unit 提供了名为 `UnitBehaviourEditor` 的 Custom Inspector。在 Unity 编辑器运行时，它接管了 Unit 的面板。

*   **颜色编码**：
    *   🟢 **绿色**：资源充足、技能就绪。
    *   🟡 **黄色**：技能冷却中。
    *   🔴 **红色**：Buff 即将过期、资源不足。
*   **实时监控**：
    *   不需要由程序员打印 Log，设计师可以直接在 Inspector 看到 Attribute 的数值跳动和 Mark 的进度条流逝。

### 17. 实时数据监控与过滤

*   **Filter 搜索框**：
    *   当单位拥有 50 个属性和 20 个状态时，面板会很乱。
    *   在搜索框输入 "Crit"，面板将自动折叠并仅显示包含 "Crit" 的属性、Buff 和反应器。
*   **技能红绿灯**：
    *   技能列表左侧的小圆点指示了技能的可用性。
    *   如果不可用，右侧会显示原因（如 "CD: 2.5s" 或 "Cost"）。

---

## 第七章：最佳实践与规范

### 18. 性能优化指南

1.  **静态 GameTag**：
    *   不要在 Update 中写 `unit.HasTag("Stun")`。这会产生隐式转换开销。
    *   请定义 `static readonly GameTag Stun = "Stun";` 并复用它。
2.  **减少 Mark 的 Tick**：
    *   只有需要每帧执行逻辑（如 DOT）的 Mark 才需要重写 `OnTick`。纯属性加成的 Mark 不需要 Tick，这能显著减少 `Update` 的开销。
3.  **事件池的正确使用**：
    *   永远使用 `EventManager.Publish<T>(initializer)`。
    *   永远不要在回调之外持有 `EventInfo` 的引用，因为该对象会被回收并复用于下一次事件。

### 19. 代码编写规范 (Do's and Don'ts)

*   **✅ Do**: 在 Reaction 中判断 `evt.Target == _owner`。虽然基类过滤了无关事件，但作为 Source（攻击者）和 Target（受击者）收到的事件是一样的，你需要区分你的角色。
*   **✅ Do**: 使用 `AttributeLinker` 处理属性间的依赖（如力量增加血上限）。不要手动写 Update 同步。
*   **⛔ Don't**: 不要直接修改 `Attributes["HP"].Value`。永远通过 `Reaction` 响应 `CombatEvent` 来修改。否则你的无敌盾、减伤甲都将失效。
*   **⛔ Don't**: 不要在 `OnExecute` 中发布同类型的事件且不加终止条件。这会导致无限递归（A 反伤 B，B 反伤 A...）。解决方案是给反伤事件加个 Tag `"Reflected"` 并不再响应它。

---

**GoveKits.Unit** 不仅仅是一套代码，更是一种思维方式。它强迫开发者将逻辑拆解为原子的、可组合的部分。遵循本手册，你将构建出稳健、可扩展且高性能的游戏核心。
**GoveKits.Unit** 不仅仅是一套代码，更是一种思维方式。它强迫开发者将逻辑拆解为原子的、可组合的部分。遵循本手册，你将构建出稳健、可扩展且高性能的游戏核心。
