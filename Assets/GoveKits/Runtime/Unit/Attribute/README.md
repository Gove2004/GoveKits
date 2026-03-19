# Runtime Unit Attribute 开发手册

Attribute 管理角色数值，支持上限绑定、数值变化和修饰器叠加。

## 设计理念

- 状态值与运行值分离，避免语义混乱。
- 修饰器独立建模，便于调试和回放。
- 变更通过容器入口统一处理。

## 架构介绍

- StateAttribute: 常驻基准值
- RuntimeAttribute: 带上下限的当前值
- AttributeModifier: 加法/乘法等修饰
- AttributeContainer: 属性注册与读写入口

## 快速开始

### 1. 建立最大值与当前值

```csharp
using GoveKits.Runtime.Unit;

var attrs = new AttributeContainer();
var maxHp = attrs.AddState("Attr.MaxHp", 100f);
attrs.AddRuntime("Attr.Hp", maxHp);
attrs.Change("Attr.Hp", -15f);
```

### 2. 应用和移除 Buff

```csharp
using GoveKits.Runtime.Unit;

var buff = new AttributeModifier(ModifierType.Multiplicative, 0.2f);
attrs.ApplyModifier("Attr.MaxHp", buff);
float hpCap = attrs.GetValue("Attr.MaxHp");
attrs.Modify("Attr.MaxHp", buff);
```

## 注意事项

- 不要直接绕过容器修改属性内部值。
- Runtime 属性会自动钳制到 `[0, MaxValue]`。
- 动态创建大量修饰器时，注意对象复用。

## 常见故障排查

- 现象: 数值变化不符合预期。
	- 排查: 检查修饰器类型和叠加顺序（加法/乘法）是否正确。
- 现象: HP 变成负值或超过上限。
	- 排查: 确认是否通过容器接口修改，而不是直接改内部字段。
- 现象: Buff 移除后属性未恢复。
	- 排查: 检查是否保存并正确移除了对应 `AttributeModifier` 实例。

## 相关跳转

- Unit: [../README.md](../README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



