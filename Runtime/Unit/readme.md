# Unit 模块

数值驱动的角色能力框架。为游戏中的"单位"（角色、怪物、NPC、世界本身）提供一套可组合的属性、技能、状态标记与反应链系统。所有行为通过 **Intent → Reaction → Effect** 管道编排，不直接耦合具体逻辑。

## 核心概念

| 概念 | 说明 |
|------|------|
| **IUnit** | 所有单位的契约，聚合四个容器（Attributes/Marks/Abilities/Reactions） |
| **Ability** | 主动技能，执行后产生 Intent，不直接修改任何状态 |
| **Reaction** | 被动反应，消费 Intent 并产出 Effect |
| **Mark** | 持续状态（Buff / Debuff / DOT），可堆叠、可 Tick |
| **Attribute** | 基础数值（HP、ATK、DEF…），支持加法 / 乘法 / 覆盖三种修饰模式 |
| **Intent** | 未解析的操作指令，经 Reaction 消费后才真正生效 |
| **Effect** | 原子状态变更，通过对象池分配，批量应用后回收 |

设计原则：**主动技能只生成意图，被动反应只消费意图并产出效果，数值变更只通过修饰器管道。** 三层解耦使技能、装备、Buff、环境效果可以自由叠加而不互相干扰。

## 架构总览

```
UnitCore（注册中心 + 工厂）
  ├── RegisterAbility<T>(tag) — 注册技能类型
  ├── RegisterMark<T>(tag)    — 注册标记类型
  ├── RegisterReaction<T>(tag) — 注册反应类型
  ├── CreateAbility(tag)      — 查注册表反射创建
  ├── CreateAbility<T>()      — 直接 new T（fluent）
  ├── CreateMark(tag)         — 查注册表反射创建
  ├── CreateMark<T>()         — 直接 new T（fluent）
  ├── CreateReaction(tag)     — 查注册表反射创建
  ├── CreateReaction<T>()     — 直接 new T（fluent）
  ├── CreateIntent<T>(src, tgt) — 对象池 + 自动设 Source/Target
  └── CreateEffect<T>()       — 对象池

IUnit（单位实例）
  ├── Attributes  → AttributeContainer
  ├── Marks       → MarkContainer
  ├── Abilities   → AbilityContainer
  └── Reactions   → ReactionContainer
  └── IUnitExtensions（扩展方法）

能力执行流:
  IUnit.UseAbility(tag, ctx)
    → Ability.Check()   (规则检查: CD / 消耗 / 属性门槛)
    → Ability.TryExecuteAsync()
    → Ability.GenerateIntentsAsync()  → IReadOnlyList<UnitIntent>
    → Intent (HurtIntent / RecoverIntent / AddMarkIntent …)
    → Target.HandleIntent(intent)
    → ReactionChain.Sort(Priority desc)
    → Reaction.CanHandle(intent)      (按 CanHandleTypes 过滤)
    → Reaction.Handle(intent, effects)
    → ApplyEffects()                   (批量应用，原子化)

生命周期流:
  UnitBehaviour.Update(dt)
    → Marks.UpdateMarks(dt)
      → TickMark.OnTick()   (DOT 每回合触发 Intent)
      → Mark 过期 → RemoveMark
```

## 快速开始

### 1. 注册技能 / 标记 / 反应

在初始化阶段（如 `GoveCore.Setup()` 之后）调用 `UnitCore`：

```csharp
// 注册一个火焰球技能（Tag 自动池化）
UnitCore.RegisterAbility<Fireball>("Skill_Fireball");

// 注册灼烧 Debuff
UnitCore.RegisterMark<BurnMark>("Debuff_Burn");

// 注册伤害反应
UnitCore.RegisterReaction<DamageReaction>("Reaction_Damage");

// Tag 相同字符串返回同一实例（String Pool 语义）
UnitTag a = "fire";
UnitTag b = "fire";
// ReferenceEquals(a, b) == true
```

### 2. 创建单位

```csharp
// 方式 A：手动创建
var unit = gameObject.AddComponent<UnitBehaviour>();
unit.InitAttributes();
unit.InitMarks();
unit.InitAbilities();
unit.InitReactions();

// 方式 B：从存档数据恢复
var archive = LoadFromDisk();
UnitSerializer.Restore(unit, archive);
```

### 3. 初始化容器内容

继承 `UnitBehaviour` 并重写初始化方法：

```csharp
public class PlayerUnit : UnitBehaviour
{
    protected override void InitAttributes()
    {
        Attributes.Add("HP", 1000f);
        Attributes.Add("ATK", 150f);
        Attributes.Add("DEF", 50f);
        Attributes.Add("SPD", 100f);
    }

    protected override void InitAbilities()
    {
        // 方式 A：通过类型直接创建（fluent API）
        var fireball = UnitCore.CreateAbility<Fireball>();
        fireball.AddRule(new CDRule("CD_Fireball", 2.0f));
        Abilities.AddAbility(fireball);

        // 方式 B：通过字符串 Tag 查注册表创建（数据驱动）
        var sword = UnitCore.CreateAbility("Skill_Sword");
        Abilities.AddAbility(sword);
    }

    protected override void InitReactions()
    {
        Reactions.AddReaction(new DamageReaction());
        Reactions.AddReaction(new DodgeReaction());
    }
}
```

### 4. 释放技能

```csharp
var context = new AbilityContext(targetUnit);
await sourceUnit.UseAbility("Skill_Fireball", context);
```

### 4.1 统一工厂 API

所有类型都支持 **类型创建**（代码硬编码）和 **字符串创建**（数据驱动/配置表）两种模式：

```csharp
// 技能 — 类型创建（直接 new T）
var ability = UnitCore.CreateAbility<Fireball>();

// 标记 — 类型创建（直接 new T，stack/duration 由子类 fluent 控制）
var mark = UnitCore.CreateMark<BurnMark>();

// 反应 — 类型创建
var reaction = UnitCore.CreateReaction<DamageReaction>();

// Intent — 对象池 + 自动设 Source/Target
var hurtIntent = UnitCore.CreateIntent<HurtIntent>(attacker, defender);

// Effect — 对象池
var effect = UnitCore.CreateEffect<HurtEffect>();
```

### 5. UnitTag 池化

```csharp
UnitTag a = "fire";
UnitTag b = "fire";
UnitTag c = new UnitTag("fire");
// a == b == c（Equals 和引用都相同）
// ReferenceEquals(a, b) == true
// UnitTag.PoolSize == 1（只占用一个实例）
```

## 属性系统

### 计算公式

```
CurrentValue = (BaseValue + ΣAdditive) × (1 + ΣMultiplicative)
```

`ModifierType.Override` 直接忽略上述公式，使用 `Value` 作为最终值。

### 属性修改器

```csharp
var mod = new AttributeModifier(
    type: ModifierType.Additive,
    value: 50f,
    source: null
);
unit.Attributes.AddModifier("ATK", mod);
```

### 数值变更管道

```csharp
// 在 InitAttributes() 之后挂载
container.BeforeValueChange = (tag, newValue) =>
{
    // 钳制、校验、阻止非法变更
    return Mathf.Max(0f, newValue);
};

container.AfterValueChange = (tag, oldValue, newValue) =>
{
    // 驱动 UI、检查死亡阈值
    if (tag == "HP" && newValue <= 0) Die();
};
```

## 技能系统 (Ability)

### 技能生命周期

```
OnInit() → CanExecute() → TryExecuteAsync() → GenerateIntentsAsync() → IReadOnlyList<UnitIntent>
```

一个技能可以产出多个 Intent。

### 规则系统 (AbilityRule)

规则在 `Check()` 时检查，通过后在 `Commit()` 执行前置操作（扣 MP、上 CD 标记等）：

```csharp
public class MPCostRule : AbilityRule
{
    private readonly float _cost;
    public override bool Check(AbilityContext ctx)
        => ctx.Source.Attributes.GetValue(new UnitTag("MP")) >= _cost;
    public override void Commit(AbilityContext ctx)
        => ctx.Source.Attributes.ChangeBase(new UnitTag("MP"), -_cost);
}
```

### 内置效果类型

`Effect.cs` 提供 9 种可直接使用的效果：

| 效果 | 作用 |
|------|------|
| `AttributeChangeEffect` | 修改属性的基础值或当前值 |
| `AttributeModifierAddEffect` | 给指定属性添加修饰器 |
| `AttributeModifierRemoveEffect` | 移除指定属性的修饰器 |
| `MarkAddEffect` | 给目标添加标记 |
| `MarkRemoveEffect` | 移除目标的指定标记 |
| `AbilityAddEffect` | 给目标添加技能 |
| `AbilityRemoveEffect` | 移除目标的指定技能 |
| `ReactionAddEffect` | 给目标添加反应 |
| `ReactionRemoveEffect` | 移除目标的指定反应 |

## 标记系统 (Mark)

### 基础 Mark

继承 `UnitMark`，实现关键生命周期：

```csharp
public class BurnMark : UnitMark
{
    public override UnitTag Name { get; protected set; } = new UnitTag("Burn");
    public override int MaxStack => 3;

    public override void OnApply() { /* 首次施加 */ }
    public override void OnStack(UnitMark newMark) { /* 重复施加 */ }
    public override void OnUpdate(float deltaTime) { /* 每帧更新 */ }
    public override void OnRemove() { /* 移除清理 */ }
}
```

### TickMark（周期性触发）

继承 `TickMark` 实现 DOT（持续伤害）等按周期触发的效果：

```csharp
public class PoisonMark : TickMark
{
    protected override void OnTick()
    {
        var intent = UnitCore.CreateIntent<HurtIntent>(Source, Owner)
            .SetDamage(damageAmount);
        Owner.HandleIntent(intent);
    }
}
```

### 堆叠与过期

- 同一 Tag 的标记重复施加时自动调用 `OnStack`，堆叠数上限由 `MaxStack` 控制
- `MarkContainer` 内部使用缓存列表避免遍历时修改集合导致的 `InvalidOperationException`
- 过期标记在下一个 `UpdateMarks(dt)` 调用中被安全移除

## 反应系统 (Reaction)

### 反应生命周期

```
CanHandle(intent) → Handle(intent, effects)
```

### Intent 过滤

Reaction 通过 `CanHandleTypes` 数组声明自己能处理的 Intent 类型，默认值为 `new[] { Name }`。`ReactionChain` 在遍历时调用 `CanHandle(intent)` 匹配 `intent.Type` 是否在数组中。

子类可以：
- 重写 `CanHandleTypes` 指定能处理的 Intent 类型列表
- 重写 `CanHandle(UnitIntent intent)` 做自定义过滤

### 优先级排序

`ReactionChain` 在每次增删 Reaction 时按 `Priority` 降序排序，高优先级的 Reaction 先处理 Intent。

### DelegateReaction（匿名反应）

无需单独定义类，用 Fluent API 快速构建：

```csharp
var reaction = DelegateReaction.Create()
    .SetName(new UnitTag("CustomShield"))
    .SetPriority(10)
    .SetCanHandle(intent => intent is HurtIntent)
    .SetAction((intent, effects) =>
    {
        var hurt = intent as HurtIntent;
        if (hurt.Damage < shieldValue)
            effects.Add(UnitCore.CreateEffect<AttributeModifierAddEffect>()
                .Set(new UnitTag("HP"), new AttributeModifier(ModifierType.Additive, 100f)));
    });

unit.Reactions.AddReaction(reaction);
```

## 标签查询 (TagQuery)

所有容器都实现了 `ITagSource`，支持统一的标签组合查询：

```csharp
// 基础查询
TagQuery q1 = TagQuery.Has("Debuff_Poison");
TagQuery q2 = TagQuery.Has("Skill_Fireball");

// 组合：中毒 OR 流血
TagQuery q3 = q1 | q2;

// 组合：非免疫 AND (中毒 OR 流血)
TagQuery q4 = !TagQuery.Has("Immune") & q3;
```

运算符重载：
- `\|` — Any（任一满足）
- `&` — All（全部满足）
- `!` — Not（取反）

## 序列化与存档

`UnitSerializer` 负责在运行时对象与纯数据 DTO 之间转换：

```csharp
// 提取数据（用于网络同步或存档）
UnitArchiveData archive = UnitSerializer.Extract(unit);
string json = JsonUtility.ToJson(archive);
File.WriteAllText(savePath, json);

// 恢复数据
UnitArchiveData loaded = JsonUtility.FromJson<UnitArchiveData>(json);
UnitSerializer.Restore(unit, loaded);
```

`UnitArchiveData` 是可序列化的纯数据结构，包含属性快照、标记列表、技能列表和反应列表，可直接序列化到 JSON / MessagePack / 任意格式。

## 世界单元 (Universe)

`Universe` 是一个全局单例 `IUnit`，代表整个世界服务器级别的 Buff 效果：

```csharp
// 初始化 Universe 容器
Universe.Instance.InitAttributes();
Universe.Instance.InitMarks();
Universe.Instance.InitAbilities();
Universe.Instance.InitReactions();

// 全服双倍经验 Buff
var mark = UnitCore.CreateMark<BuffDoubleExp>();
mark.Apply(Universe.Instance);

// 关闭时清理
Universe.DestroyInstance();
```

## 扩展指南

### 新增技能

1. 继承 `UnitAbility`，实现 `Name` 属性
2. 重写 `GenerateIntentsAsync()` 产出 `IReadOnlyList<UnitIntent>`
3. 可选：在 `OnInit()` 中添加 `AbilityRule`
4. 在初始化时 `UnitCore.RegisterAbility<MySkill>("tag")`
5. 通过 `unit.UseAbility("tag", ctx)` 调用

### 新增标记

1. 继承 `UnitMark` 或 `TickMark`
2. 实现生命周期回调（`OnApply` / `OnStack` / `OnUpdate` / `OnRemove` / `OnTick`）
3. `UnitCore.RegisterMark<MyMark>("tag")`
4. 通过 `MarkAddEffect` 或直接 `Marks.AddMark()` 施加

### 新增反应

1. 继承 `UnitReaction`，实现 `Handle(intent, effects)`
2. 默认 `CanHandleTypes = new[] { Name }`，如需处理多种 Intent 类型则重写 `CanHandleTypes`
3. 可选：重写 `CanHandle(UnitIntent intent)` 做自定义过滤
4. `UnitCore.RegisterReaction<MyReaction>("tag")`
5. 通过 `Reactions.AddReaction()` 挂载到单位

### 新增效果

1. 继承 `UnitEffect<T>`（CRTP + 对象池自动管理）
2. 实现 `OnApply()` 和 `OnRecycle()`
3. 在反应的 `Handle()` 中添加到 `effects` 列表即可

## 文件结构

```
Unit/
  Ability/
    AbilityContainer.cs      # 技能容器
    UnitAbility.cs           # 技能基类
    AbilityContext.cs        # 执行上下文
    AbilityRule.cs           # 技能规则抽象
  Attribute/
    AttributeContainer.cs    # 属性容器
    UnitAttribute.cs         # 属性数据块
    AttributeModifier.cs     # 属性修饰器
  Mark/
    MarkContainer.cs         # 标记容器
    UnitMark.cs              # 标记基类 + TickMark
  Reaction/
    ReactionContainer.cs     # 反应容器
    ReactionChain.cs         # 反应链（优先级排序 + 分发）
    UnitReaction.cs          # 反应基类
    DelegateReaction.cs      # 匿名反应构建器
  Tag/
    ITagSource.cs            # 标签查询契约
    TagQuery.cs              # 组合条件查询
    UnitTag.cs               # 池化字符串标签（class）
  Unit/
    IUnit.cs                 # IUnit 接口
    IUnitExtensions.cs       # IUnit 扩展方法
    Universe.cs              # 世界单例
    UnitCore.cs              # 注册中心 + 工厂
    UnitIntent.cs            # Intent 基类
    UnitEffect.cs            # Effect 基类 (泛型/非泛型)
    UnitSerializer.cs        # 序列化 / 反序列化
  Extension/
    UnitBehaviour.cs         # MonoBehaviour 宿主
    CD.cs                    # CDRule + CDMark
    Effect.cs                # 内置效果实现 (9 种)
```
