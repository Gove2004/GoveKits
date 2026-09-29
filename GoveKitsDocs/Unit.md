# Runtime/Unit —— 类 GAS 能力系统

面向单位的技能/属性/状态/反应框架（GAS-like）：属性容器、技能执行、状态标记（Buff/Debuff）、反应链、效果应用，全部由 `UnitCore` 工厂统一创建。

## 模块架构

```
Runtime/Unit/
├── Unit/             核心 —— UnitCore 工厂注册 / IUnit 接口 / Universe 世界容器
├── Attribute/        属性 —— 属性容器、属性值、修正器（Modifier）
├── Ability/          技能 —— 技能基类、技能容器、执行上下文、规则（冷却等）
├── Mark/             标记 —— Buff/Debuff 基类、Tick 标记、标记容器
├── Reaction/         反应 —— 基于全局事件总线（EventCore）的被动监听系统
├── Tag/              标签 —— UnitTag（string 池化）与查询
└── Extension/        扩展 —— 效果（Effect）、冷却（CD）、MonoBehaviour 载体
```

## 概念

```
主动方: 技能执行 Abilities.TryExecuteAsync → ExecuteAsync 里 EventCore.Publish(事件)
                                    ↓
被动方: 反应 UnitReaction<T> 订阅同一事件 → OnFilter 过滤 → OnEvent 产出 Effect
                                    ↓
效果: Effect.OnApply → 修改目标属性 / 挂标记 / 挂技能（即用即还池）
```

- **UnitTag**：`"hp"` 隐式转 UnitTag（池化，相同字符串同一实例），全框架用它做键
- **容器**：每个单位（`IUnit`）持有 4 个容器：属性 / 标记 / 技能 / 反应
- **效果**：池化（`UnitEffect<T>`），`XxxEffect.Create().Set(...).Apply(target)` 即用即还
- **事件**：池化（`EventData`），`EventCore.Pick<T>()` 取 → 填数据 → `EventCore.Publish(evt)` 发布并自动回收
- **解耦**：技能不认识被动，被动也不认识技能，双方只通过事件类型约定耦合

## 完整示例（战士砍击：伤害 + 冷却 + 流血）

```csharp
// 1. 事件：伤害事件（自定义事件继承 EventData，发布后自动入池回收）
public class DamageEvent : EventData
{
    public IUnit Source;
    public IUnit Target;
    public float Damage;

    public override void OnRecycle()        // 必须重置，避免复用时脏数据
    {
        Source = null; Target = null; Damage = 0f;
    }
}

// 2. 单位（继承 UnitBehaviour，Awake 自动初始化四大容器）
public class Hero : UnitBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        Attributes.Add("hp", 100);          // 初始属性（"hp" 隐式转 UnitTag）
        Attributes.Add("atk", 10);

        Abilities.AddAbility(UnitCore.CreateAbility<SlashAbility>());   // 挂技能
    }
}
public class Enemy : UnitBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        Attributes.Add("hp", 200);
        Reactions.AddReaction(UnitCore.CreateReaction<BleedReaction>()); // 挂被动（挂上即开始监听）
    }
}

// 3. 技能：砍击（发布伤害事件，不关心谁会响应）
public class SlashAbility : UnitAbility
{
    public override UnitTag Name => "slash";

    protected override void OnInit() => AddRule(new CDRule("slash_cd", 3f));   // 3 秒冷却

    public override UniTask ExecuteAsync(AbilityContext context, CancellationToken ct)
    {
        var evt = EventCore.Pick<DamageEvent>();        // 从池中取事件
        evt.Source = Owner;
        evt.Target = context.Target;
        evt.Damage = Owner.GetValue("atk");             // 取属性用 IUnit 扩展（容器上只有带 Func 的 GetValue）
        EventCore.Publish(evt);                         // 发布 → 所有订阅者被触发，随后自动回收
        return UniTask.CompletedTask;
    }
}

// 4. 反应：受击后结算伤害并挂流血（被动，无需技能感知）
public class BleedReaction : UnitReaction<DamageEvent>
{
    public override UnitTag Name => "passive_bleed";
    public override int Priority => 0;                             // 值越大越先执行

    public override bool OnFilter(DamageEvent e) => e.Target == Owner;   // 只处理打自己的事件

    public override void OnEvent(DamageEvent e)
    {
        AttributeChangeEffect.Create().Set("hp", -e.Damage).Apply(Owner);           // 结算伤害
        MarkAddEffect.Create().Set(UnitCore.CreateMark<BleedMark>()).Apply(Owner);  // 挂流血
    }
}

// 5. 标记：流血（每 1 秒掉 5 血，持续 3 秒）
public class BleedMark : TickMark
{
    public override UnitTag Name { get; protected set; } = "bleed";

    public override void OnApply()
    {
        base.OnApply();                 // 必须：TickMark.OnApply 会重置周期计时器（标记走池化复用）
        Duration = 3f;                  // 持续 3 秒（protected set，子类可设）
        SetInterval(1f);                // 每 1 秒 tick 一次
    }
    protected override void OnTick()    // 每次 tick：掉血
        => AttributeChangeEffect.Create().Set("hp", -5f).Apply(Owner);
}

// 6. 使用
var hero = GetComponent<Hero>();
var enemy = GetComponent<Enemy>();
var ctx = new AbilityContext(hero, enemy);
await hero.Abilities.TryExecuteAsync("slash", ctx);   // 执行技能 → 发事件 → 被动响应 → 效果落地
```

## 各子模块用法

### Attribute —— 属性

```csharp
var attrs = unit.Attributes;

attrs.Add("hp", 100);                               // 定义属性（基值）
float hp = unit.GetValue("hp");                     // 取当前值（IUnit 扩展，Func 可省略）
attrs.GetBaseValue("hp");                           // 取基值（容器方法）
attrs.ChangeBase("hp", -10);                        // 直接改基值

// 修正器（Modifier）：临时加成，可移除
public class EquipSource : ModifierSource { }       // 自定义来源（区分装备/Buff 等）

var mod = new AttributeModifier(ModifierType.Additive, 5f, new EquipSource());
attrs.AddModifier("atk", mod);
attrs.RemoveModifier("atk", source);                // 按来源移除：优先 Source 引用精确匹配
attrs.RemoveModifier("atk", new EquipSource());     // 引用未命中时回退按 Source 类型移除该类型的全部修改器

// 变更回调
attrs.BeforeValueChange += (tag, newValue) => Mathf.Clamp(newValue, 0f, 999f);  // 预变更拦截：Func<UnitTag, float, float>，必须返回修正后的合法值
attrs.AfterValueChange  += (tag, oldV, newV) => { };                            // 后变更通知：Action<UnitTag, float, float>
```

### Ability —— 技能

```csharp
// 泛型直接创建（推荐，无需注册）
var skill = UnitCore.CreateAbility<SlashAbility>();
// 或按 Tag 工厂创建（需先 RegisterAbility<T>(tag)）
UnitCore.RegisterAbility<SlashAbility>("slash");
var skill2 = UnitCore.CreateAbility("slash");

skill.AddRule(new CDRule("slash_cd", 3f));          // 冷却规则（3 秒，期间技能被拦截）

// 自定义规则（继承 AbilityRule，实现 Check/Commit）
public class ManaCostRule : AbilityRule
{
    public override bool Check(AbilityContext context)
        => context.Source.GetValue("mp") >= 10;
    public override void Commit(AbilityContext context)
        => AttributeChangeEffect.Create().Set("mp", -10f).Apply(context.Source);
}
skill.AddRule(new ManaCostRule());
skill.RemoveRule(rule);

var ctx = new AbilityContext(source, target);       // 执行上下文（可传参数）
ctx.SetFloat("power", 2f);                          // 上下文传参
await skill.TryExecuteAsync(ctx, cancellationToken);  // 执行（内部先跑规则检查，再调 ExecuteAsync）
```

### Mark —— 标记（Buff/Debuff）

```csharp
var mark = UnitCore.CreateMark<BleedMark>();        // 创建标记（Duration/Stack 在子类 OnApply 中设定）
unit.Marks.AddMark(mark);                           // 施加（触发 OnApply）

bool has = unit.Marks.HasTag("bleed");              // 检测
unit.Marks.RemoveMark("bleed");                     // 移除
var m = unit.Marks.GetMark<BleedMark>("bleed");     // 获取

// 每帧驱动标记 Tick（UnitBehaviour 已内置，也可手动）
void Update() => unit.UpdateUnit(Time.deltaTime);
```

标记生命周期：`OnApply`（施加）→ `OnUpdate`（每帧/tick）→ `OnStack`（叠层）→ `OnRemove`（移除）。

### Reaction —— 反应（被动，订阅全局事件）

反应 = 常驻的事件监听器：继承 `UnitReaction<T>` 实现 `OnEvent` 即可，订阅与注销由容器自动完成。

```csharp
// 写法一：继承（有状态、可复用，推荐）
public class ThornsReaction : UnitReaction<DamageEvent>
{
    public override UnitTag Name => "passive_thorns";
    public override int Priority => 10;                                  // 值越大越先执行
    public override bool OnFilter(DamageEvent e) => e.Target == Owner;   // 只响应打自己的事件
    public override void OnEvent(DamageEvent e)
        => AttributeChangeEffect.Create().Set("hp", -10f).Apply(e.Source);   // 反弹 10 点
}

// 写法二：委托流式装配（临时、轻量，无需新建类型）
var thorns = new DelegateReaction<DamageEvent>()
    .SetName("passive_thorns")
    .SetPriority(10)
    .SetFilter(e => e.Target == unit)
    .SetAction(e => AttributeChangeEffect.Create().Set("hp", -10f).Apply(e.Source));

// 挂载 / 开关 / 卸载（挂载即激活 = 开始监听，卸载自动注销）
unit.Reactions.AddReaction(UnitCore.CreateReaction<ThornsReaction>());  // 或 AddReaction(thorns)
unit.Reactions.Enable("passive_thorns", false);      // 封印度：注销订阅，暂停响应
unit.Reactions.Enable("passive_thorns", true);       // 解封：重新订阅
unit.Reactions.RemoveReaction("passive_thorns");     // 卸载并注销
```

拦截/改写事件：`OnFilter` 里改字段（如把伤害减半）再放行，或直接 `e.IsBreak = true` 中断后续监听器执行。
优先级说明：同一事件下按 `Priority` 从大到小依次调用，所以「减伤」这类反应应设高优先级，先于「结算伤害」执行。

### Effect —— 效果（池化，即用即还）

```csharp
// 内置效果
AttributeChangeEffect.Create().Set("hp", -10f).Apply(target);       // 改属性
MarkAddEffect.Create().Set(bleedMark).Apply(target);                // 挂标记
MarkRemoveEffect.Create().Set("bleed").Apply(target);               // 移除标记
AttributeModifierAddEffect.Create().Set("atk", mod).Apply(target);  // 加修正器

// 自定义效果
public class HealEffect : UnitEffect<HealEffect>
{
    public float Amount;
    public HealEffect Set(float amount) { Amount = amount; return this; }
    public override void OnApply<TUnit>(TUnit target) => target.Attributes.ChangeBase("hp", Amount);
    public override void OnRecycle() => Amount = 0;                 // 归还池时重置
}
HealEffect.Create().Set(50f).Apply(unit);
```

### Universe —— 世界容器

```csharp
Universe.Instance.InitAttributes();               // 初始化容器（同 IUnit 四件套）
Universe.Instance.Update(Time.deltaTime);          // 手动驱动标记刷新
```

### Serializer —— 存档/读档（UnitSerializer）

```csharp
var data = UnitSerializer.Extract(unit);      // 提取全部状态为纯数据（可 JSON 序列化）
UnitSerializer.Restore(unit, data);           // 从数据重建（内部先 unit.Clear() 清空）
```

- **属性**：入档 `BaseValue` 与修改器列表（类型 + 数值），读档恢复基值后重挂修改器、重算当前值。
  限制：`ModifierSource` 是运行时对象引用，无法序列化——读档后修改器的 Source 为 `null`，
  只能通过 `RemoveModifier(tag, new XxxSource())` 按 Source **类型**移除，无法按原引用精确移除。
- **标记**：入档层数 / 持续时间 / 计时进度；读档时在 `AddMark` 之后恢复 `Timer`，Buff 进度不丢失。
- **技能 / 反应**：只入档标签，读档时经 `UnitCore` 工厂重建（需提前 Register）。
- 存档中出现未注册的标签时会跳过该项并输出错误日志，不会中断整体读档流程。
