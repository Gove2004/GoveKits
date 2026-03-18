# Runtime Unit Attribute 开发文档

Attribute 模块负责角色数值体系。

## 设计理念

- 上限与当前值分离。
- 修改器规则透明化。
- 数值变化可观测。

## 架构介绍

- UnitAttribute.cs: 属性基类 + StateAttribute + RuntimeAttribute
- AttributeModifier.cs: 修改器定义
- AttributeContainer.cs: 属性注册与变更入口

## 快速开始

```csharp
var attrs = new AttributeContainer();
var maxHp = attrs.AddState("MaxHp", 100f);
var hp = attrs.AddRuntime("Hp", maxHp);

attrs.ApplyModifier("MaxHp", new AttributeModifier(ModifierType.Additive, 20f));
attrs.ApplyChange("Hp", -35f);
```

## 注意事项

- `StateAttribute.Value` 不可直接写。
- `RuntimeAttribute` 会自动钳制到 [0, MaxValue]。
- 不再使用时调用 `AttributeContainer.Clear()`。

## 相关跳转

- Unit: [../README.md](../README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
