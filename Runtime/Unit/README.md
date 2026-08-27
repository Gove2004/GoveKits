# Runtime/Unit —— 类 GAS 能力系统

面向单位的技能/属性/状态/反应框架（GAS-like）：属性容器、技能执行、状态标记（Buff/Debuff）、反应链、效果应用，全部由 `UnitCore` 工厂统一创建。

## 模块架构

```
Runtime/Unit/
├── Unit/             核心 —— UnitCore 工厂注册 / IUnit 接口 / Universe 世界容器
├── Attribute/        属性 —— 属性容器、属性值、修正器（Modifier）
├── Ability/          技能 —— 技能基类、技能容器、执行上下文、规则（冷却等）
├── Mark/             标记 —— Buff/Debuff 基类、Tick 标记、标记容器
├── Reaction/         反应 —— 拦截 Intent 并产生效果的被动系统
├── Tag/              标签 —— UnitTag（string 池化）与查询
└── Extension/        扩展 —— 效果（Effect）、冷却（CD）、MonoBehaviour 载体
```

## 概念

```
技能执行: Abilities.TryExecuteAsync → GenerateIntents 生成 Intent
→ 反应系统 Reactions 拦截处理 Intent → 产生 Effect → Effect.OnApply 修改目标属性/标记
```

- **UnitTag**：`"hp"` 隐式转 UnitTag（池化，相同字符串同一实例），全框架用它做键
- **容器**：每个单位（`IUnit`）持有 4 个容器：属性 / 标记 / 技能 / 反应
- **效果**：池化（`UnitEffect<T>`），`XxxEffect.Create().Set(...).Apply(target)` 即用即还

## 完整示例（战士砍击：伤害 + 冷却 + 流血）

```csharp
// 1. 单位（继承 UnitBehaviour，Awake 自动初始化四大容器）
public class Hero : UnitBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        Attributes.Add("hp", 100);          // 初始属性（"hp" 隐式转 UnitTag）
        Attributes.Add("atk", 10);

        Abilities.AddAbility(UnitCore.CreateAbility<SlashAbility>());   // 挂技能
        Reactions.AddReaction(UnitCore.CreateReaction<BleedReaction>()); // 挂反应
    }
}

// 2. 技能：砍击（生成伤害意图 → 反应系统处理）
public class SlashAbility : UnitAbility
{
    protected override async UniTask<IReadOnlyList<UnitIntent>> GenerateIntentsAsync(AbilityContext context, CancellationToken ct)
    {
        var dmg = context.Source.Attributes.GetValue("atk");
        var intent = UnitCore.CreateIntent<SlashIntent>();   // 池化创建（勿直接 new）
        intent.Damage = dmg;
        return new UnitIntent[] { intent };
    }
}
public class SlashIntent : UnitIntent
{
    public override UnitTag Type => "slash";     // 意图类型标签
    public float Damage;
}

// 3. 反应：拦截砍击意图，对目标施加伤害 + 流血标记
public class BleedReaction : UnitReaction
{
    public override bool CanHandle(UnitIntent intent) => intent is SlashIntent;

    public override void Handle(UnitIntent intent, IList<UnitEffect> effects)
    {
        var slash = (SlashIntent)intent;
        effects.Add(AttributeChangeEffect.Create().Set("hp", -slash.Damage));   // 直接伤害
        effects.Add(MarkAddEffect.Create().Set(UnitCore.CreateMark<BleedMark>())); // 挂流血
    }
}

// 4. 标记：流血（每 1 秒掉 5 血，持续 3 秒）
public class BleedMark : TickMark
{
    public override UnitTag Name { get; protected set; } = "bleed";

    public override void OnApply()
    {
        Duration = 3f;                  // 持续 3 秒（protected set，子类可设）
        SetInterval(1f);                // 每 1 秒 tick 一次
    }
    public override void OnUpdate(float deltaTime)  // 每 tick：掉血
        => AttributeChangeEffect.Create().Set("hp", -5f).Apply(Owner);
}

// 5. 使用
var hero = GetComponent<Hero>();
var enemy = GetComponent<Enemy>();
var ctx = new AbilityContext(hero, enemy);
await hero.Abilities.TryExecuteAsync("slash", ctx);   // 执行技能 → 反应 → 效果自动应用
```

## 各子模块用法

### Attribute —— 属性

```csharp
var attrs = unit.Attributes;

attrs.Add("hp", 100);                               // 定义属性（基值）
float hp = attrs.GetValue("hp");                    // 取当前值
attrs.ChangeBase("hp", -10);                        // 直接改基值

// 修正器（Modifier）：临时加成，可移除
public class EquipSource : ModifierSource { }       // 自定义来源（区分装备/Buff 等）

var mod = new AttributeModifier(ModifierType.Additive, 5f, new EquipSource());
attrs.AddModifier("atk", mod);
attrs.RemoveModifier("atk", new EquipSource());     // 按来源移除（同一来源类型）

// 变更回调
attrs.BeforeValueChange += (tag, oldV, newV) => { };
attrs.AfterValueChange  += (tag, oldV, newV) => { };
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
        => context.Source.Attributes.GetValue("mp") >= 10;
    public override void Commit(AbilityContext context)
        => AttributeChangeEffect.Create().Set("mp", -10f).Apply(context.Source);
}
skill.AddRule(new ManaCostRule());
skill.RemoveRule(rule);

var ctx = new AbilityContext(source, target);       // 执行上下文（可传参数）
ctx.SetFloat("power", 2f);                          // 上下文传参
await skill.TryExecuteAsync(ctx, cancellationToken);  // 执行（内部走规则检查 → 意图 → 反应）
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

### Reaction —— 反应（被动拦截）

```csharp
public class DamageShield : UnitReaction
{
    public override bool CanHandle(UnitIntent intent) => intent is SlashIntent;
    public override void Handle(UnitIntent intent, IList<UnitEffect> effects)
    {
        // 拦截伤害：把伤害减半再交给后续
        ((SlashIntent)intent).Damage *= 0.5f;
    }
}
unit.Reactions.AddReaction(UnitCore.CreateReaction<DamageShield>());
unit.Reactions.Enable("damage_shield", false);      // 开关反应
```

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
