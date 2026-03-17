# Attribute Module

Attribute 用于管理 Unit 的数值属性，例如：

- 最大生命值、攻击力、移动速度（StateAttribute）
- 当前生命值、当前蓝量（RuntimeAttribute）

支持通过 Modifier 动态调整数值，并在值变化时触发事件通知。

## 1. 核心结构

### 1.1 UnitAttribute

所有属性类型的基类，封装：

- `Name`（UnitTag）：属性唯一标识
- `Value`：当前属性值
- `OnValueChanged`：值变更事件（oldValue, newValue）

### 1.2 StateAttribute

静态上限属性（如 MaxHp、Atk），支持修改器叠加：

- `BaseValue`：基础值，可直接赋值
- `AddModifier(modifier)`：叠加修改器，返回可释放句柄
- `RemoveModifier(modifier)`：移除修改器
- 结算公式：`final = (base + ΣAdditive) × (1 + ΣMultiplicative)` → Override 覆盖
- 结果受 `minLimit` / `maxLimit` 约束

### 1.3 RuntimeAttribute

运行时当前值（如 CurrentHp），以 StateAttribute 为上限：

- `MaxValue`：读取关联 StateAttribute 的 Value
- `Ratio`：当前值占上限的比例 [0, 1]
- `Change(delta)`：增减当前值，自动钳制到 [0, MaxValue]
- 当上限变化时自动重新钳制当前值

### 1.4 AttributeModifier

修改器结构体，包含：

- `Type`：Additive（加法）/ Multiplicative（乘法）/ Override（覆盖）
- `Value`：修改量
- `Source`：来源（可为 null）

### 1.5 AttributeContainer

属性容器，统一管理所有属性的注册与查询：

- `AddState` / `GetState`：注册 / 获取状态属性
- `AddRuntime` / `GetRuntime`：注册 / 获取运行时属性
- `ApplyModifier` / `RemoveModifier`：对状态属性应用 / 移除修改器
- `ApplyChange`：对运行时属性执行增量修改
- `GetValue`：按名称读取属性当前值（不存在时返回 0）
- `Clear`：清空并释放所有属性

## 2. 修改器公式

```
final = (BaseValue + ΣAdditive) × (1 + ΣMultiplicative)
```

若存在 Override 修改器，最终结果直接覆盖为 Override 值。
结果始终约束在 `[minLimit, maxLimit]` 范围内。

## 3. 快速开始

下面是一个最小的角色属性示例：为英雄注册 MaxHp / CurrentHp / Atk，并挂载一个装备 Modifier。

```csharp
using System;
using UnityEngine;
using GoveKits.Runtime.Unit;

public sealed class DemoUnit : MonoBehaviour, IUnit
{
    public AttributeContainer Attributes { get; } = new AttributeContainer();
    public MarkContainer Marks { get; } = new MarkContainer();

    private StateAttribute _maxHp;
    private RuntimeAttribute _currentHp;
    private StateAttribute _atk;

    private void Awake()
    {
        // 注册状态属性：MaxHp 基础值 100，最小 0，最大 9999
        _maxHp = Attributes.AddState("MaxHp", baseValue: 100f, minLimit: 0f, maxLimit: 9999f);
        // 注册运行时属性：CurrentHp 绑定 MaxHp 为上限
        _currentHp = Attributes.AddRuntime("CurrentHp", _maxHp);
        // 注册攻击力
        _atk = Attributes.AddState("Atk", baseValue: 20f);

        // 订阅 HP 变化事件
        _currentHp.OnValueChanged += OnHpChanged;
    }

    private void Update()
    {
        Marks.UpdateMarks(Time.deltaTime);
    }

    [ContextMenu("Equip Sword")]
    private void EquipSword()
    {
        // Additive +15 攻击力
        var mod = new AttributeModifier(ModifierType.Additive, 15f);
        Attributes.ApplyModifier("Atk", mod);
        Debug.Log($"Atk after equip: {_atk.Value}"); // 35
    }

    [ContextMenu("Take Damage")]
    private void TakeDamage()
    {
        // 扣减当前血量
        Attributes.ApplyChange("CurrentHp", -30f);
    }

    private void OnHpChanged(float oldVal, float newVal)
    {
        Debug.Log($"HP: {oldVal} → {newVal}  Ratio: {_currentHp.Ratio:P0}");
    }

    private void OnDestroy()
    {
        _currentHp.OnValueChanged -= OnHpChanged;
        Attributes.Clear();
    }
}
```

### 使用 ModifierSource 追踪来源

```csharp
using GoveKits.Runtime.Unit;

// 定义来源，方便批量移除同一装备的所有 Modifier
public sealed class SwordSource : ModifierSource { }

public static class AttributeExample
{
    public static void ApplyEquipment(AttributeContainer attributes)
    {
        var sword = new SwordSource();

        var atkMod = new AttributeModifier(ModifierType.Additive, 15f, sword);
        // AddModifier 返回 IDisposable 句柄，Dispose 时自动移除
        var handle = attributes.GetState("Atk")?.AddModifier(atkMod);

        // 卸下装备时：
        handle?.Dispose();
    }
}
```

## 4. 注意事项

- `StateAttribute.Value` 只读，修改数值请使用 `BaseValue` 或修改器接口。
- `AddModifier` 返回的 `DisposeAction` 建议妥善保存，以便在正确时机卸载。
- 不使用属性时调用 `AttributeContainer.Clear()` 释放事件引用，避免内存泄漏。
- `RuntimeAttribute` 的当前值在上限缩减时会自动向下钳制，不需要手动处理。
