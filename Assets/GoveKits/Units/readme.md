
---

# 📘 GoveKits.Units 开发指南

**GoveKits.Units** 是一套基于 **UniTask** 的高性能、响应式 RPG 游戏框架。它不依赖传统的 ECS，而是采用现代化的面向对象设计，提供了强大的**属性依赖图**、**异步能力系统**、**逻辑化 Buff** 以及 **声明式 Effect 构建器**。

---

## 🛠️ 前置需求 (Dependencies)

本框架深度依赖 **Cysharp/UniTask** 来处理异步逻辑（冷却、持续效果、序列执行）。
*   请确保项目中已安装 [UniTask](https://github.com/Cysharp/UniTask)。

---

## 🚀 快速开始 (Quick Start)

### 1. 创建单位 (Create a Unit)
你可以直接实例化 `Unit` 类，或者挂载 `UnitComponent` 到 GameObject 上。

```csharp
// 方法 A: 纯代码模式 (适合非实体对象)
IUnit player = new Unit();

// 方法 B: MonoBehaviour 模式 (适合场景物体)
IUnit enemy = gameObject.AddComponent<UnitComponent>();
```

### 2. 初始化属性 (Setup Attributes)
使用 `AppendLinear` 快速构建经典的 **Base(白值) * Factor(百分比) + Bias(附加值)** 结构。

```csharp
// 初始化生命值：基础100，无加成
player.Attributes.AppendLinear("Health", 100f);

// 初始化攻击力：基础10
player.Attributes.AppendLinear("Attack", 10f);

// 监听 UI 刷新
player.Attributes.AddListener("Health", (oldVal, newVal) => {
    Debug.Log($"玩家血量变化: {oldVal} -> {newVal}");
    // updateHPBar(newVal);
});
```

### 3. 赋予能力 (Grant Ability)
```csharp
// 创建一个火球术能力
var fireball = new FireballAbility();

// 赋予玩家
player.Abilities.Add("Fireball", fireball);
```

### 4. 执行战斗 (Combat)
```csharp
// 构建上下文
var context = new UnitContext(source: player, target: enemy);

// 尝试释放技能
await player.Abilities.TryExecute("Fireball", context);
```

---

## 🧠 核心模块详解

### 1. 属性系统 (Attribute System)
属性不仅仅是数字，而是一个**动态依赖图**。

*   **获取值**：`unit.Attributes.TryGetValue("Health", out float hp);`
*   **修改值**：
    *   **永久提升**：修改 `_Base` 属性。
    *   **Buff 加成**：修改 `_Factor` 或 `_Bias` 属性。
    *   **直接修改**（如扣血）：`unit.Attributes.SetValue("Health", current - damage);`

**高级用法：响应式公式**
支持运算符重载，自动构建依赖关系。当 `Attack` 变化时，`CombatPower` 会自动更新并通知 UI。
```csharp
var atk = player.Attributes.Get("Attack");
var def = player.Attributes.Get("Defense");

// 战斗力 = 攻击力 + 防御力 * 2
var combatPower = (atk + (def * 2f)).As("CombatPower");
player.Attributes.Add("CombatPower", combatPower);
```

### 2. Buff 系统 (Buff System)
Buff 是带有逻辑的 Tag。

*   **添加 Buff**：
    ```csharp
    // 添加中毒 Buff，初始 1 层
    unit.buffs.Add("Poison", new PoisonBuff()); 
    ```
*   **Buff 查询 (BuffQuery)**：
    使用流式 API 进行复杂的逻辑判断（类似于 UE GameplayTags）。
    ```csharp
    var canCastUlt = BuffQueryBuilder.All(
        BuffQueryBuilder.None("Silenced"),  // 没有沉默
        BuffQueryBuilder.None("Stunned"),   // 没有眩晕
        BuffQueryBuilder.Has("PoweredUp")   // 拥有强化状态
    );

    if (unit.buffs.MatchQuery(canCastUlt)) { ... }
    ```

### 3. 能力系统 (Ability System)
基于 `UniTask` 的全异步生命周期。

自定义技能示例：
```csharp
public class FireballAbility : BaseAbility
{
    public FireballAbility() : base("Fireball") 
    {
        CooldownTime = 5.0f; // 5秒冷却
    }

    public override async UniTask Execute(UnitContext context)
    {
        // 1. 播放动画 (使用 Effect 构建器)
        await UnityEffectBuilder.PlayAnimation(animator, "Cast_Fireball").Apply(context);

        // 2. 延迟 0.5秒 (前摇)
        await BaseEffectBuilder.After(0.5f).Apply(context);

        // 3. 造成伤害 (属性修改)
        float dmg = context.Source.Attributes.Get("Attack").Value * 2.0f;
        await UnitEffectBuilder.DecreaseAttribute(
            context.Target.Attributes, "Health", dmg
        ).Apply(context);
    }
}
```

### 4. 效果构建器 (Effect Builder)
将游戏逻辑“积木化”，支持串行、并行、条件判断。

```csharp
// 组合一个复杂的受击效果
IEffect hitEffect = BaseEffectBuilder.Sequence(
    // 1. 并行播放特效和音效
    BaseEffectBuilder.Parallel(
        UnityEffectBuilder.PlayParticle(bloodVFX),
        UnityEffectBuilder.PlayAudio(hitSFX)
    ),
    // 2. 击退效果 (假设有 MoveEffect)
    UnityEffectBuilder.Move(targetTransform, knockbackPos, 0.2f),
    // 3. 如果血量低于 30%，触发红屏警告
    BaseEffectBuilder.If(
        ctx => ctx.Target.Attributes.Get("Health").Value < 30,
        UnityEffectBuilder.SetActive(redScreenWarning, true)
    )
);

await hitEffect.Apply(context);
```

---

## 💡 最佳实践与注意事项

### ⚠️ 1. 避免运行时的公式构建
Attribute 的运算符重载（如 `attrA + attrB`）会创建新的 Attribute 实例。
*   **✅ 正确做法**：在 `Awake/Init` 阶段构建好所有公式（依赖图）。
*   **❌ 错误做法**：在 `Update` 或技能执行中动态创建公式。这会导致 GC 压力。

### ⚡ 2. 冷却系统优化
目前的 `Cooldown` 实现使用了 `while` 循环。如果单位极多，建议优化为时间戳比对法（记录 `EndTime`），以降低 CPU 开销。

### 🔗 3. 动态装备属性 (HP = 50% ATK)
如果需要实现“攻击力转化生命值”的装备效果，请勿直接修改 Health 的公式。
**推荐做法**：
1.  使用 `AppendLinear` 初始化 Health。
2.  获取 `Health_Bias` 属性。
3.  创建一个依赖于 Attack 的新属性，并修改 `Health_Bias` 的值（或手动相加）。
*(注：当前框架版本属性公式一旦确定即只读，建议通过 Buff 或 Effect 监听 Attack 变化并手动修正 Health_Bias)*

### 🔄 4. 循环依赖
框架内置了 `HasCircularDependency` 检测。在构建复杂公式（如 A依赖B，B依赖A）时，如果检测到闭环会抛出异常，请确保属性流向是单向的（例如：一级属性 -> 二级属性 -> 战斗力）。

---

## 📁 目录结构建议

```
Assets/GoveKits/Units/
├── Core/
│   ├── Unit.cs
│   ├── UnitContext.cs
│   └── IUnit.cs
├── Attribute/
│   ├── Attribute.cs
│   └── AttributeContainer.cs
├── Ability/
│   ├── IAbility.cs
│   └── AbilityContainer.cs
├── Buff/
│   ├── Buff.cs
│   └── BuffQuery.cs
└── Effect/
    ├── IEffect.cs
    ├── BaseEffectBuilder.cs (逻辑控制)
    └── UnityEffectBuilder.cs (Unity相关)
```