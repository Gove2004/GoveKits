
# GoveKits Unit 模块

GoveKits Runtime Unit 是游戏单位开发框架，采用组件化架构设计，提供属性管理、技能系统、状态标记、事件反应等核心功能。开箱即用，与 Core 模块深度整合。

## 目录结构

```
Unit/
├── IUnit.cs              # Unit 统一接口与基类
├── Universe.cs           # 全局单位单例
├── Ability/              # 技能系统
│   ├── UnitAbility.cs    # 技能基类
│   ├── AbilityRule.cs    # 技能规则基类
│   ├── AbilityContext.cs # 技能执行上下文
│   └── AbilityContainer.cs # 技能容器
├── Attribute/            # 属性系统
│   ├── UnitAttribute.cs  # 属性数据
│   ├── AttributeModifier.cs # 属性修改器
│   └── AttributeContainer.cs # 属性容器
├── Mark/                 # 状态标记系统
│   ├── UnitMark.cs       # 标记基类
│   └── MarkContainer.cs  # 标记容器
├── Reaction/             # 事件反应系统
│   ├── UnitReaction.cs   # 反应基类
│   ├── DelegateReaction.cs # 委托反应实现
│   └── ReactionContainer.cs # 反应容器
├── Util/                 # 工具组件
│   ├── UnitEffect.cs     # 效果基类
│   ├── UnitTag.cs        # 标签类型
│   └── TagQuery.cs       # 标签查询系统
└── Extension/            # 扩展组件
    ├── UnitBehaviour.cs  # Unity 组件行为
    ├── Effect.cs         # 预设效果实现
    └── CD.cs             # 冷却系统
```

## 架构设计

```
┌─────────────────────────────────────────────────────────┐
│                      IUnit 接口                         │
│  • 统一暴露四大容器  • 提供标准行为方法  • 支持生命周期  │
└─────────────────────────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┬───────────────┐
           ▼               ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ AttributeCont.  │ │   MarkCont.     │ │ AbilityCont.    │ │ ReactionCont.   │
│  • 属性管理     │ │  • 状态标记     │ │  • 技能管理     │ │  • 事件反应     │
│  • 修改器系统   │ │  • 持续效果     │ │  • 规则检查     │ │  • 事件订阅     │
└─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘
           │               │               │               │
           └───────────────┼───────────────┼───────────────┘
                           ▼               ▼
                   ┌─────────────────┐ ┌─────────────────┐
                   │ AbilityContext  │ │   UnitEffect    │
                   │  • 执行上下文   │ │  • 效果应用    │
                   │  • 源/目标管理  │ │  • 对象池集成  │
                   └─────────────────┘ └─────────────────┘
```

## 1. IUnit 统一接口

Unit 系统的核心契约，约定四个核心容器的暴露方式和标准行为方法。

### 核心组件

|组件名|	说明|
|---|---|
|Attributes|	属性容器 - 管理生命值、攻击力等数值|
|Marks|	标记容器 - 管理 Buff/Debuff 等状态|
|Abilities|	技能容器 - 管理技能注册与执行|
|Reactions|	反应容器 - 管理事件响应逻辑|

### 使用示例

```csharp
// ===== 1. 实现 IUnit 接口 =====
public class Character : BaseUnit  // 继承抽象基类实现接口
{
    // BaseUnit 已实现基本容器创建逻辑
    // 可重写 Init* 方法自定义初始化逻辑
    public override void InitAttributes()
    {
        base.InitAttributes();
        // 注册初始属性
        Attributes.Add("Health", 100);
        Attributes.Add("Attack", 50);
    }
    
    public override void InitMarks()
    {
        base.InitMarks();
        // 可添加特殊标记处理逻辑
    }
}

// ===== 2. 访问容器 =====
var character = new Character();
character.InitAttributes();
character.InitMarks();
character.InitAbilities();
character.InitReactions();

// 访问属性值
float health = character.Value("Health");
float attack = character.Attributes.GetValue("Attack");

// 应用效果
character.ApplyEffect(damageEffect);

// 执行技能
var context = new AbilityContext(character, target);
character.Use("FireBall", context);
```

### 注意事项

- 推荐继承 BaseUnit 而非直接实现 IUnit
- 容器初始化必须在使用前完成
- Value() 方法是获取属性值的便捷方式
- ApplyEffect() 会自动回收效果对象到池中
- 所有容器都是懒加载，首次访问时创建

## 2. AttributeContainer 属性容器

集中管理单位的所有数值属性，支持修改器系统和值变更通知。

### 核心特性

|特性|	说明|
|---|---|
|标签化|	使用 UnitTag 作为属性标识|
|修改器|	支持 Additive/Multiplicative/Override 三种类型|
|拦截器|	支持 Before/After 值变更钩子|
|事件化|	支持值变更通知回调|

### 使用示例

```csharp
// ===== 1. 基本操作 =====
var attributes = new AttributeContainer();

// 添加属性
attributes.Add("Health", 100f);
attributes.Add("Attack", 50f);

// 获取值
float health = attributes.GetValue("Health");

// 修改基础值
attributes.ChangeBase("Health", -10f);  // 扣血

// ===== 2. 修改器系统 =====
// 创建修改器
var bonus = new AttributeModifier(
    ModifierType.Additive, 
    20f, 
    new ModifierSource()  // 需要自定义来源类
);

// 添加修改器
attributes.AddModifier("Attack", bonus);

// 移除修改器
attributes.RemoveModifier("Attack", modifierSource);

// ===== 3. 拦截器 =====
attributes.BeforeValueChange = (tag, value) => 
{
    // 钳制数值范围
    if (tag == "Health")
        return Mathf.Clamp(value, 0, 999);
    return value;
};

attributes.AfterValueChange = (tag, oldValue, newValue) => 
{
    // 值变更通知
    if (tag == "Health" && newValue <= 0)
    {
        // 角色死亡逻辑
    }
};
```

### 注意事项

- 修改器来源需要继承 ModifierSource 类
- BeforeValueChange 用于数值校验和钳制
- AfterValueChange 用于响应值变更事件
- ChangeBase 会触发完整的计算管线
- 修改器移除时只有真正移除才会触发重算

## 3. MarkContainer 标记容器

管理单位的状态标记（Buff/Debuff），支持持续时间、堆叠和周期性触发。

### 核心特性

|功能|	说明|
|---|---|
|持续时间|	支持定时自动移除|
|堆叠机制|	支持层数叠加|
|周期触发|	TickMark 支持定期执行逻辑|
|自动管理|	UpdateMarks 自动处理过期标记|

### 使用示例

```csharp
// ===== 1. 自定义标记 =====
public class PoisonMark : TickMark
{
    public int DamagePerTick { get; private set; }
    
    public PoisonMark(IUnit owner, int damagePerTick, float duration, float tickInterval) 
        : base(owner, tickInterval, duration: duration)
    {
        DamagePerTick = damagePerTick;
        Name = "Poison";
    }
    
    protected override void OnTick()
    {
        // 每 tick 造成伤害
        var damageEffect = AttributeChangeEffect.Create()
            .Set("Health", -DamagePerTick);
        Owner.ApplyEffect(damageEffect);
    }
}

// ===== 2. 使用标记 =====
var marks = new MarkContainer();

// 添加标记
var poison = new PoisonMark(character, 10, 5f, 1f);  // 每秒掉10血，持续5秒
marks.AddMark(poison);

// 更新标记（需要在 Update 中调用）
marks.UpdateMarks(Time.deltaTime);

// 获取标记
var existingPoison = marks.GetMark<PoisonMark>("Poison");

// 移除标记
marks.RemoveMark("Poison");
```

### 注意事项

- TickMark 继承自 UnitMark，支持周期性逻辑
- 需要在 Update 中调用 UpdateMarks 处理过期标记
- OnStack 实现自定义堆叠逻辑
- 标记过期后会在 UpdateMarks 中自动移除
- Name 属性必须在构造函数中设置

## 4. AbilityContainer 技能容器

管理单位的技能注册、执行和生命周期。

### 核心特性

|功能|	说明|
|---|---|
|技能注册|	支持添加/移除技能实例|
|规则检查|	支持前置条件检查|
|异步执行|	支持协程执行技能逻辑|
|生命周期|	自动管理技能资源|

### 使用示例

```csharp
// ===== 1. 自定义技能 =====
public class FireBallAbility : UnitAbility
{
    public override UnitTag Name => "FireBall";
    
    public FireBallAbility(IUnit owner) : base(owner) { }
    
    public override async UniTask ExecuteAsync(AbilityContext context, CancellationToken cancellationToken = default)
    {
        // 技能执行逻辑
        var damage = Owner.Value("Attack") * 1.5f;
        
        var damageEffect = AttributeChangeEffect.Create()
            .Set("Health", -damage);
        damageEffect.Apply(context.Target);
        
        await UniTask.Yield();  // 模拟异步操作
    }
}

// ===== 2. 使用技能容器 =====
var abilities = new AbilityContainer();

// 添加技能
var fireball = new FireBallAbility(character);
abilities.AddAbility(fireball);

// 执行技能
var context = new AbilityContext(character, targetCharacter);
await abilities.TryExecuteAsync("FireBall", context);

// 添加规则（如冷却）
fireball.AddRule(new CDRule("CD.FireBall", 3f));

// 获取技能
var skill = abilities.GetAbility<FireBallAbility>("FireBall");
```

### 注意事项

- 技能名称必须唯一
- CanExecute 检查所有前置规则
- TryExecuteAsync 包含完整的执行流程
- 技能执行时会自动提交规则副作用
- 通过 AddRule 添加执行前检查规则

## 5. ReactionContainer 反应容器

管理单位对事件的响应逻辑，基于事件系统实现。

### 核心特性

|功能|	说明|
|---|---|
|事件订阅|	自动管理事件订阅/取消|
|优先级|	支持反应执行优先级|
|过滤器|	支持事件过滤逻辑|
|激活管理|	支持反应的启用/禁用|

### 使用示例

```csharp
// ===== 1. 自定义事件 =====
public class DamageEvent : EventData
{
    public IUnit Attacker { get; set; }
    public IUnit Target { get; set; }
    public float Damage { get; set; }
    
    public override void OnRecycle()
    {
        Attacker = null;
        Target = null;
        Damage = 0;
    }
}

// ===== 2. 自定义反应 =====
public class DamageReaction : UnitReaction<DamageEvent>
{
    public override UnitTag Name => "DamageReaction";
    public override int Priority => 10;  // 高优先级
    
    public DamageReaction(IUnit owner) : base(owner) { }
    
    public override bool OnFilter(DamageEvent eventInfo)
    {
        // 只处理针对自己的伤害
        return eventInfo.Target == Owner;
    }
    
    public override void OnEvent(DamageEvent eventInfo)
    {
        // 受伤后触发逻辑
        var visualEffect = VisualEffect.Create().Set("HitFlash");
        Owner.ApplyEffect(visualEffect);
    }
}

// ===== 3. 使用反应容器 =====
var reactions = new ReactionContainer();

// 添加反应
var damageReact = new DamageReaction(character);
reactions.AddReaction(damageReact);

// 激活/禁用反应
reactions.Enable("DamageReaction", false);

// 使用委托反应（快速实现）
var delegateReact = new DelegateReaction<DamageEvent>(
    character, 
    "QuickReaction", 
    (e) => Debug.Log($"受到伤害: {e.Damage}")
);
reactions.AddReaction(delegateReact);
```

### 注意事项

- 事件类必须继承 EventData 并重写 OnRecycle
- 反应自动订阅/取消订阅事件
- OnFilter 实现事件过滤逻辑
- Priority 数值越大优先级越高
- 反应激活状态可通过容器统一管理

## 6. UnitEffect 效果系统

提供即时效果的统一接口，与对象池集成以提高性能。

### 使用示例

```csharp
// ===== 1. 自定义效果 =====
public class VisualEffect : UnitEffect<VisualEffect>
{
    public string EffectName { get; private set; }
    
    public VisualEffect Set(string effectName)
    {
        EffectName = effectName;
        return this;
    }
    
    public override void OnApply<TUnit>(TUnit target)
    {
        // 播放视觉效果
        Debug.Log($"播放效果: {EffectName} -> {target.GetType().Name}");
    }
    
    public override void OnRecycle()
    {
        EffectName = null;
    }
}

// ===== 2. 使用效果 =====
// 创建效果实例（从池中获取）
var visualEffect = VisualEffect.Create().Set("HealAnimation");

// 应用到单位
character.ApplyEffect(visualEffect);  // 自动回收到池中

// 预设效果
var damageEffect = AttributeChangeEffect.Create()
    .Set("Health", -50f);
character.ApplyEffect(damageEffect);

var addModifierEffect = AttributeModifierAddEffect.Create()
    .Set("Attack", new AttributeModifier(ModifierType.Additive, 10f, new ModifierSource()));
character.ApplyEffect(addModifierEffect);

// 对于持久化效果，使用 ApplyWithoutPool 避免自动回收
var persistentEffect = new PersistentEffect();
persistentEffect.ApplyWithoutPool(target);
```

### 注意事项

- 效果类必须继承 UnitEffect<T>
- Set 方法用于参数配置，支持链式调用
- OnApply 实现具体效果逻辑
- OnRecycle 重置所有字段
- Apply 自动回收到池中，ApplyWithoutPool 不回收

## 完整示例

### 战斗角色实现

```csharp
// ===== 1. 角色定义 =====
public class BattleCharacter : UnitBehaviour  // 继承 Unity 行为组件
{
    [Header("初始属性")]
    public float maxHealth = 100f;
    public float attack = 50f;
    
    protected override void Awake()
    {
        base.Awake();  // 初始化容器
        
        // 初始化属性
        Attributes.Add("Health", maxHealth);
        Attributes.Add("MaxHealth", maxHealth);
        Attributes.Add("Attack", attack);
        
        // 添加初始反应
        Reactions.AddReaction(new DeathReaction(this));
    }
    
    // 便捷方法
    public bool IsAlive() => Value("Health") > 0;
    
    public void TakeDamage(float damage)
    {
        var context = new AbilityContext(this);
        var damageEffect = AttributeChangeEffect.Create()
            .Set("Health", -damage);
        ApplyEffect(damageEffect);
    }
}

// ===== 2. 死亡反应 =====
public class DeathReaction : UnitReaction<AttributeChangeEvent>
{
    public override UnitTag Name => "DeathReaction";
    public override int Priority => 100;
    
    public DeathReaction(IUnit owner) : base(owner) { }
    
    public override bool OnFilter(AttributeChangeEvent e)
    {
        return e.AttributeKey == "Health" && 
               e.NewValue <= 0 && 
               e.OldValue > 0;
    }
    
    public override void OnEvent(AttributeChangeEvent e)
    {
        // 角色死亡逻辑
        var character = (BattleCharacter)((UnitBehaviour)e.Owner).gameObject;
        character.GetComponent<Animator>().SetTrigger("Die");
    }
}

// ===== 3. 使用 =====
// 在场景中挂载 BattleCharacter 组件
var character = GetComponent<BattleCharacter>();

// 造成伤害
character.TakeDamage(60f);  // 生命降到40

// 使用技能
var context = new AbilityContext(character, target);
character.Use("FireBall", context);
```

## 通用注意事项

### 最佳实践

- 组件命名：使用 功能 + Container/Mark/Ability/Reaction 格式
- 标签命名：使用 域.功能 格式，如 "Stat.Health"、"CD.Skill1"
- 效果复用：通过 Set 方法配置参数，支持池化复用
- 事件设计：事件类只包含必要数据，避免复杂逻辑
- 内存管理：标记和技能自动管理生命周期，无需手动清理
- 性能优化：大量使用对象池避免 GC，合理使用 Update 频率

### 扩展方向

- 属性系统：扩展更多 ModifierType 类型
- 技能系统：实现更复杂的技能组合机制
- 事件系统：集成更高级的事件过滤和聚合
- AI 集成：结合 AI 模块实现智能决策
- 网络同步：扩展网络状态同步机制
- 数据驱动：通过配置文件定义属性和技能
