# Unit Module

`GoveKits.Runtime.Unit` 提供一套轻量的单位基础层，当前重点包含：

- `UnitTag`：标签值类型（替代裸字符串）
- `TagQuery`：可组合的标签查询表达式（`!` / `&` / `|`）
- `StateAttribute` / `RuntimeAttribute`：状态值与运行时值
- `AttributeContainer`：统一属性容器
- `IUnitReaction` / `UnitReaction<T>`：事件驱动反应（被动技能）
- `ReactionContainer`：反应容器（增删、启停、清理）
- `IUnit`：单位接口（暴露 `Attributes`）

## 1. 目录结构

```text
Runtime/Unit/
├─ Tag/
│  ├─ UnitTag.cs
│  └─ TagQuery.cs
├─ Attribute/
│  ├─ AttributeModifier.cs
│  ├─ UnitAttribute.cs
│  └─ AttributeContainer.cs
├─ Reaction/
│  ├─ UnitReaction.cs
│  ├─ ReactionContainer.cs
│  └─ README.md
├─ IUnit.cs
├─ Ability/UnitAbility.cs      (占位)
├─ Mark/UnitMark.cs            (占位)
└─ Effect/UnitEffect.cs        (占位)
```

## 2. UnitTag

`UnitTag` 是基于字符串语义的值类型，支持：

- 隐式转换：`string -> UnitTag`、`UnitTag -> string`
- 作为字典键使用（缓存哈希）
- `==` / `!=` 比较

示例：

```csharp
UnitTag hpTag = "Hp";
UnitTag atkTag = "Atk";

if (hpTag != atkTag)
{
	// ...
}
```

## 3. TagQuery

`TagQuery` 用于构建标签匹配条件树，查询源需实现 `IUnitTagSource`：

```csharp
public interface IUnitTagSource
{
	bool HasTag(UnitTag tag);
}
```

支持语法糖：

- `TagQuery q = "Stunned";`
- `!q`（NOT）
- `q1 & q2`（AND）
- `q1 | q2`（OR）

也支持工厂方法：`TagQuery.All(...)` / `TagQuery.Any(...)` / `TagQuery.Not(...)` / `TagQuery.Custom(...)`。

示例：

```csharp
TagQuery canCast = !"Silenced" & !"Stunned";
bool ok = canCast.Match(tagSource);
```

## 4. Attribute 系统

### 4.1 UnitAttribute

属性基类，提供：

- `Name`：属性标识（`UnitTag`）
- `Value`：属性值（可被派生类重写）
- `OnValueChanged(oldValue, newValue)`：值变更事件

### 4.2 StateAttribute

用于最大生命、攻击力等“状态值”。

关键点：

- `Value` 是计算结果，只读（直接写会抛异常）
- 可写入口是 `BaseValue`
- 修改器通过 `AddModifier/RemoveModifier` 管理
- 脏标记 + 缓存：`Refresh()` 时按需重算

当前公式：

```text
final = (base + sumAdd) * (1 + sumMuty) -> override
```

其中：

- `Additive` 累加到 `sumAdd`
- `Multiplicative` 累加到 `sumMuty`（`0.1` 表示 +10%）
- 若存在 `Override`，最终值直接覆盖为 `overrideValue`

### 4.3 RuntimeAttribute

用于当前生命、当前法力等“运行时值”。

关键点：

- 绑定一个 `StateAttribute` 作为上限来源
- `Value` 自动钳制到 `[0, MaxValue]`
- 提供便捷操作：`Change(delta)`、`Full()`、`Clear()`
- 上限变化时自动重钳制

## 5. AttributeModifier

修改器类型：

- `Additive`
- `Multiplicative`
- `Override`

可选来源：`ModifierSource`（用于追踪来源/后续批量移除）。

## 6. AttributeContainer

`AttributeContainer` 是属性总入口，并实现了 `IUnitTagSource`。

常用能力：

- State：`AddState/GetState/ApplyModifier/RemoveModifier`
- Runtime：`AddRuntime/GetRuntime/ApplyChange`
- 通用：`GetValue(name)`、`Clear()`
- Tag 查询支持：`HasTag(tag)`（根据是否存在对应属性判断）

## 7. IUnit

当前 `IUnit` 定义为：

```csharp
public interface IUnit
{
	AttributeContainer Attributes { get; }
}
```

建议单位对象实现该接口，将属性系统作为统一数据入口。

## 8. Reaction（被动技能）

Reaction 模块用于实现“事件触发型被动”：

- 通过 `UnitReaction<T>` 监听指定事件类型
- 通过 `DelegateReaction<T>` 快速挂载委托逻辑
- 通过 `ReactionContainer` 统一管理反应生命周期

常用流程：

1. 创建反应实例（继承类或委托版）
2. `AddReaction` 注册到容器
3. `SetActive(true)` 启动监听
4. 在事件触发时自动执行 `OnReaction`
5. 销毁时 `Clear()` 释放全部订阅

详细示例见 [Reaction/README.md](Reaction/README.md)。

## 9. 快速示例

```csharp
// 1) 创建容器
var attrs = new AttributeContainer();

// 2) 添加状态属性（最大生命）
var maxHp = attrs.AddState("MaxHp", baseValue: 100f);

// 3) 添加运行时属性（当前生命）
var hp = attrs.AddRuntime("Hp", maxHp);

// 4) 应用修改器（+20 最大生命）
attrs.ApplyModifier("MaxHp", new AttributeModifier(ModifierType.Additive, 10));

// 5) 扣血
attrs.ApplyChange("Hp", -35f);

// 6) 标签查询（基于容器是否存在对应属性）
TagQuery q = "Hp" & "MaxHp";
bool pass = q.Match(attrs);
```

## 10. 注意事项

- `Ability/Mark/Effect` 目录当前仍是占位，后续可基于本模块继续扩展。
- Reaction 模块已可用于被动技能场景，建议优先通过 `ReactionContainer` 管理生命周期。
