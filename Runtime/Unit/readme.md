
# GoveKits Unit 模块 (类 GAS 核心能力系统)

GoveKits Runtime Unit 是一套高度工业化、零 GC、支持纯数据驱动（Data-Driven）的类 GAS 游戏能力框架。
它采用 IoC（控制反转） 设计，将逻辑与宿主彻底解耦；内置极速对象池与序列化管线，完美支持复杂 RPG/SLG 游戏中的技能、Buff、被动触发、属性联动及读写档需求。

## 目录结构

```
Unit/
├── IUnit.cs                  # Unit 统一接口与基类 (实体契约)
├── Universe.cs               # 全局环境单例 (用于全局 Buff/事件)
├── UnitCore.cs               # ⭐️ 组件全局注册与工厂中心 (数据驱动核心)
├── UnitSerializer.cs         # ⭐️ 纯数据序列化与状态重建工具 (用于读写档)
├── Ability/                  # 技能系统
│   ├── UnitAbility.cs        # 技能基类 (无参构造, 支持依赖注入)
│   ├── AbilityRule.cs        # 技能执行规则 (前置条件与消耗)
│   ├── AbilityContext.cs     # 技能执行上下文 (Source, Target, 临时参数)
│   └── AbilityContainer.cs   # 技能容器
├── Attribute/                # 属性系统
│   ├── UnitAttribute.cs      # 属性数据块 (BaseValue, CurrentValue)
│   ├── AttributeModifier.cs  # 属性修改器 (0GC Struct)
│   └── AttributeContainer.cs # 属性容器 (重算管线)
├── Mark/                     # 状态标记系统 (Buff/Debuff)
│   ├── UnitMark.cs           # 标记基类 (支持堆叠, 持续时间)
│   ├── CD.cs                 # 基于 Mark 的通用冷却规则
│   └── MarkContainer.cs      # 标记容器 (生命周期管理)
├── Reaction/                 # 被动反应系统 (事件监听)
│   ├── UnitReaction.cs       # 反应基类 (事件订阅生命周期)
│   ├── DelegateReaction.cs   # 委托快捷反应实现
│   └── ReactionContainer.cs  # 反应容器
├── Util/                     # 工具与扩展
│   ├── UnitEffect.cs         # ⭐️ 即时效果基类 (CRTP模式，极速对象池)
│   ├── Effect.cs             # 内置的通用效果 (扣血、加Buff等)
│   ├── UnitTag.cs            # 极速哈希标签 (替代 String 键值)
│   └── TagQuery.cs           # ⭐️ 标签逻辑树查询 (与/或/非 组合匹配)
└── UnitBehaviour.cs          # Unity MonoBehaviour 宿主表现层实现
```

## 核心架构理念

1. IoC 依赖注入与无参构造

所有的 Ability (技能)、Mark (状态)、Reaction (被动) 均采用 无参构造函数。它们不再强依赖宿主，而是在被 Add 进容器的瞬间，由容器将 Owner 注入给它们。这使得组件可以通过工厂动态生成。

2. 数据驱动 (Data-Driven)

通过 UnitCore 注册中心，我们可以将 JSON 或配置表里的字符串标签，直接转化为游戏内的实体能力。配合 UnitSerializer，可将怪物的所有状态抽离为极简的 POCO 数据。

3. 零 GC 执行管线

数值修改器 (AttributeModifier) 采用 Struct 结构。瞬间爆发的伤害、治疗、Buff 挂载全部采用 UnitEffect<T> 结合底层 PoolCore 实现对象的极速复用，运行时绝不产生内存垃圾。

## 核心模块使用指南

### 1.UnitCore 与 数据驱动初始化

将能力标签与具体的 C# 类绑定。通常在游戏启动时执行一次。

```csharp
// 1. 在游戏启动时注册能力
UnitCore.RegisterAbility<FireBallAbility>("Skill_FireBall");
UnitCore.RegisterMark<PoisonMark>("Buff_Poison");
UnitCore.RegisterReaction<DodgeReaction>("Passive_Dodge");

// 2. 在运行时，直接通过标签实例化（工厂模式）
UnitMark poison = UnitCore.CreateMark("Buff_Poison", stack: 1, duration: 5f);
```

### 2.IUnit 与 宿主装配

使你的游戏实体继承 UnitBehaviour（或非 Unity 环境下继承 BaseUnit），它将自动初始化四大容器。

```csharp
public class Monster : UnitBehaviour
{
    protected override void Awake()
    {
        base.Awake(); // 自动初始化 Attributes, Marks, Abilities, Reactions

        // 初始化基础属性
        Attributes.Add("HP", 1000f);
        Attributes.Add("Attack", 50f);

        // 通过工厂挂载初始被动
        Reactions.AddReaction(UnitCore.CreateReaction("Passive_Dodge"));
    }
}
```

### 3. AttributeContainer (属性与修改器)

提供基础值、修改器（加、乘、覆盖）以及生命周期拦截管线。

```csharp
// 1. 添加属性修改器 (如：装备了一把加 20% 攻击力的剑)
var swordBuff = new AttributeModifier(ModifierType.Multiplicative, 0.2f, this);
monster.Attributes.AddModifier("Attack", swordBuff);

// 2. 移除修改器 (卸下装备)
monster.Attributes.RemoveModifier("Attack", this);

// 3. 属性安全钳制 (拦截管线)
monster.Attributes.BeforeValueChange = (tag, value) => {
    if (tag == "HP") return Mathf.Clamp(value, 0, monster.Attributes.GetBaseValue("MaxHP"));
    return value;
};
```

### 4. UnitEffect (瞬时效果与 0GC 对象池)

基于命令模式，处理扣血、加状态等瞬时行为。支持流畅的链式调用 API。

```csharp
// 最佳实践：使用 Create() 从对象池获取，Apply() 执行后自动回收，0GC！
AttributeChangeEffect.Create()
    .Set("HP", -150f)
    .Apply(monster); // 对怪物造成 150 点真实伤害

// 给怪物挂载一个中毒 Buff
MarkAddEffect.Create()
    .Set(UnitCore.CreateMark("Buff_Poison", stack: 1, duration: 10f))
    .Apply(monster);
```

### AbilityContainer (技能与状态机)

管理技能的执行前置条件（Rules）和异步执行过程。

```csharp
public class FireBallAbility : UnitAbility
{
    public override UnitTag Name => "Skill_FireBall";

    protected override void OnInit()
    {
        // 添加前置规则：需要 3 秒冷却时间
        AddRule(new CDRule("CD.FireBall", 3f));
    }

    public override async UniTask ExecuteAsync(AbilityContext context, CancellationToken ct)
    {
        // 1. 播放动画
        // 2. 生成火球飞行
        // 3. 命中后造成伤害
        float damage = Owner.Value("Attack") * 2.0f;
        AttributeChangeEffect.Create().Set("HP", -damage).Apply(context.Target);
    }
}

// 外部调用释放技能：
var ctx = new AbilityContext(source: player, target: monster);
await player.Use("Skill_FireBall", ctx); // 会自动检查 CD
```

### 6. ReactionContainer (被动事件订阅)

极度优雅的基于委托或类的被动事件监听器。

```csharp
// 快速流式装配一个被动反应：当收到伤害时，反弹 10 点伤害
var thornsReaction = new DelegateReaction<DamageEvent>()
    .SetName("Passive_Thorns")
    .SetPriority(10)
    .SetFilter(evt => evt.Target == Owner) // 仅拦截打自己的伤害
    .SetAction(evt => {
        AttributeChangeEffect.Create().Set("HP", -10f).Apply(evt.Source);
    });

player.Reactions.AddReaction(thornsReaction);
```

### 7. TagQuery (标签逻辑树查询)

其强大的状态查询表达式，支持 &(与), |(或), !(非)。用于技能前置条件判断。

```csharp
// 业务要求：目标必须 [没有免疫标记]，且必须处于 [中毒 或 眩晕] 状态之一
TagQuery condition = !TagQuery.Has("Buff_Immune") & (TagQuery.Has("Buff_Poison") | TagQuery.Has("Buff_Stun"));

if (condition.Match(monster.Marks))
{
    // 满足条件，触发背刺暴击！
}
```

### 8. UnitSerializer (数据驱动)

一键提取实体的所有数据（HP、剩余 CD、身上的 Buff 层数），并完美重建。

```csharp
// 1. 提取当前单位纯数据 (可直接转 JSON 存入硬盘 / 发送给服务器)
UnitArchiveData archiveData = UnitSerializer.Extract(monster);

// 2. 读档时，将数据完美灌入一个空壳单位 (自动恢复 Buff 剩余读秒)
IUnit newMonster = new Monster();
UnitSerializer.Restore(newMonster, archiveData);
```

## 最佳实践与注意事项

1. 绝对不要在 Effect 内部缓存状态：如果使用了 XXXEffect.Create().Apply()，该对象会在执行完的瞬间被底层回收。如果你需要持久化持有它，请使用 new XXXEffect().ApplyWithoutPool()。

2. TickMark 的 Update：MarkContainer.UpdateMarks(deltaTime) 必须在宿主的生命周期（如 Update）中被不断调用，否则 Buff 的持续时间和周期性掉血不会生效。

3. 扩展自定义 Effect：请继承自 UnitEffect<T> 而非非泛型的基类，这样你才能白嫖底层的 0GC 泛型对象池机制。

4. 性能规范：在代码中尽量使用 UnitTag 代替 string 进行字典查询，它在内部会预计算 Hash，查找速度极快。

