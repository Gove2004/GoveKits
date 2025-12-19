# Unit/Core 模块

单位（Unit）框架的核心组件库，为游戏单位（角色、敌人、NPC 等）提供完整的属性、能力、状态、反应系统。

## 核心概念

### 四大容器系统

每个 `GameUnit` 持有四个核心容器，相互配合构成完整的单位行为模型：

| 容器 | 职能 | 关键类 |
|------|------|--------|
| **Attributes** | 属性管理（血量、MP、攻击力等） | `GameAttribute`, `StateAttribute`, `RuntimeAttribute` |
| **Marks** | 状态标记（Buff、Debuff、冷却等） | `GameMark`, `CooldownMark`, `GameTag` |
| **Abilities** | 能力/技能系统 | `GameAbility`, `AbilityCost`, `IGameAbility` |
| **Reactions** | 被动反应/事件监听 | `GameReaction<T>`, `GameEffect`, `DelegateReaction<T>` |

---

## 模块结构

### 核心入口：GameUnit

```csharp
public interface IGameUnit
{
    AttributeContainer Attributes { get; }
    MarkContainer Marks { get; }
    AbilityContainer Abilities { get; }
    ReactionContainer Reactions { get; }
}

public abstract class GameUnit : IGameUnit
{
    // 实现四个容器的初始化与持有
}
```

**使用示例：**

```csharp
class Player : GameUnit
{
    public Player(string name)
    {
        // 初始化四大容器
        InitializeAttributes();
        InitializeMarks();
        InitializeAbilities();
        InitializeReactions();
        
        // 注册属性、能力、反应等
    }
}
```

---

## 子模块详解

### 1. Mark（状态标记系统）

**文件：** `Mark/GameMark.cs`, `Mark/GameTag.cs`, `Mark/TagQuery.cs`

**职能：** 管理单位的临时或永久状态（冷却、晕眩、加速等）。

**核心类：**

- **GameTag**：高性能字符串包装，用于快速标签比对（零分配 Dict 键）
- **GameMark**：标记基类，支持自动销毁、周期心跳、堆叠机制
- **CooldownMark**：特殊标记，用于能力冷却（不堆叠、刷新时间）
- **TagQuery**：复杂标签查询（支持 AND/OR/NOT 运算符重载）

**使用示例：**

```csharp
// 添加标记
var stunMark = new GameMark("Stunned", duration: 3.0f);
unit.Marks.Apply(stunMark, unit, unit);

// 标签查询
if (unit.Marks.HasTag("Stunned"))
{
    // 无法行动
}

// 复杂查询
var isVulnerable = ("Fragile" | "Bleeding").Match(unit.Marks);
```

---

### 2. Attribute（属性系统）

**文件：** `Attribute/GameAttribute.cs`, `Attribute/StateAttribute.cs`, `Attribute/RuntimeAttribute.cs`, `Attribute/AttributeModifier.cs`, `Attribute/AttributeLinker.cs`

**职能：** 管理单位的基础属性与派生属性。

**属性类型：**

| 类型 | 说明 | 示例 |
|------|------|------|
| **StateAttribute** | 计算型属性（支持修改器） | MaxHP、攻击力、防御力 |
| **RuntimeAttribute** | 资源型属性（当前值 ≤ Max） | 当前 HP、当前 MP |

**修改器类型：**

```csharp
public enum ModifierType
{
    Flat,        // 固定加值: Base + 5
    PercentAdd,  // 百分比叠加: × (1 + 10%)
    PercentMult, // 独立乘区: × 1.2 × 1.1
    Override     // 绝对覆写: 强制 = 100
}
```

**使用示例：**

```csharp
// 创建基础属性
var maxHp = new StateAttribute("MaxHP", baseValue: 100);
var currentHp = new RuntimeAttribute("CurrentHP", maxHp);

// 添加修改器（如装备加成）
var mod = new GameModifier(ModifierType.PercentAdd, 0.2f, source: equipment);
maxHp.AddModifier(mod);  // MaxHP 变为 120

// 属性链接（体质 -> 血量上限）
AttributeLinker.Link(stamina, maxHp, val => val * 3);

// 取伤
currentHp.ApplyChange(-20);
```

---

### 3. Ability（能力系统）

**文件：** `Ability/GameAbility.cs`, `Ability/AbilityCost.cs`, `Ability/CooldownMark.cs`

**职能：** 定义单位可执行的技能与动作。

**核心流程：**

```
CanExecute(同步检查)
  ↓
Execute(异步执行)
  ├─ 支付消耗
  ├─ 施加冷却
  └─ OnExecute(自定义逻辑)
```

**使用示例：**

```csharp
class FireballAbility : GameAbility
{
    public FireballAbility() : base("Fireball")
    {
        SetCooldown(5.0f);  // 5 秒冷却
        AddCost("MP", 30);  // 消耗 30 MP
    }

    public override async UniTask OnExecute(IGameUnit source, IGameUnit target)
    {
        // 播放动画、应用伤害等
        await UniTask.Delay(500);
        
        var damage = source.Attributes.GetValue("Atk") * 1.5f;
        target.TakeDamage(damage);
    }
}
```

---

### 4. Reaction（被动反应系统）

**文件：** `Reaction/GameReaction.cs`, `Reaction/GameEffect.cs`, `Reaction/DelegateReaction.cs`

**职能：** 监听事件总线，实现被动效果与链式反应。

**核心概念：**

- **GameEffect**：事件消息（包含来源、目标、上下文标签）
- **GameReaction**：事件监听器（激活时订阅、取消激活时取消订阅）
- **DelegateReaction**：便捷适配器（直接使用 Lambda）

**使用示例：**

```csharp
// 定义伤害反应（受伤时触发）
class OnDamagedReaction : GameReaction<DamageEffect>
{
    protected override void OnExecute(DamageEffect effect)
    {
        // 受伤时反弹伤害
        var reflected = new DamageEffect 
        { 
            Source = effect.Target,
            Target = effect.Source,
            Amount = effect.Amount * 0.2f
        };
        EventManager.Publish(reflected);
    }
}

// 或使用委托快捷方式
var reaction = new DelegateReaction<DamageEffect>(
    "OnDamaged",
    unit,
    (effect) => Debug.Log($"受到 {effect.Amount} 伤害")
);
reaction.Activate();
```

---

## 完整使用示例

```csharp
class Hero : GameUnit
{
    public override void InitializeAttributes()
    {
        // 基础属性
        var baseAtk = new StateAttribute("BaseAtk", 10);
        var maxHp = new StateAttribute("MaxHP", 100);
        var currentHp = new RuntimeAttribute("CurrentHP", maxHp);
        
        Attributes.Add(baseAtk);
        Attributes.Add(maxHp);
        Attributes.Add(currentHp);
        
        // 属性链接（等级 -> 攻击力）
        var level = new StateAttribute("Level", 1);
        AttributeLinker.Link(level, baseAtk, lv => lv * 5);
    }

    public override void InitializeAbilities()
    {
        Abilities.Add(new FireballAbility());
        Abilities.Add(new HealAbility());
    }

    public override void InitializeReactions()
    {
        // 受伤时的反应
        var reaction = new DelegateReaction<DamageEffect>(
            "OnDamaged",
            this,
            (effect) =>
            {
                var hp = Attributes.GetValue("CurrentHP");
                Attributes.ApplyRuntimeChange("CurrentHP", -effect.Amount);
                
                if (hp <= 0) OnDeath();
            }
        );
        Reactions.Add(reaction);
    }
}
```

---

## 最佳实践

1. **继承 GameUnit**：为每个单位类型（玩家、敌人、NPC）创建独立的 GameUnit 子类
2. **容器初始化**：在构造函数中调用四个 Initialize* 方法设置完整的系统
3. **属性链接**：使用 `AttributeLinker` 建立属性间的依赖关系，避免手动同步
4. **标签查询**：充分利用 `TagQuery` 的运算符重载简化复杂的条件判定
5. **事件解耦**：通过 Reaction 系统而非直接调用解耦不同模块的交互

---

## 相关文档

- [Events 事件系统](../../../Utility/Events/README.md)
- [Times 时间调度](../../../Utility/Times/README.md)
