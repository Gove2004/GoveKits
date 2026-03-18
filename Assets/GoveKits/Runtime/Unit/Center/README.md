# Runtime Unit Center 开发文档

Center 模块用于集中扫描并创建 Unit 子系统实例。

## 设计理念

- 创建逻辑集中管理。
- 注解驱动注册，减少误注册。
- 启动初始化与业务使用解耦。

## 架构介绍

- FactoryScanAttribute.cs: FactoryAutoRegisterAttribute
- AbilityCenter.cs / MarkCenter.cs / ReactionCenter.cs
- UnitCenter.cs

## 快速开始

```csharp
[FactoryAutoRegister]
public sealed class BurnMark : UnitMark
{
    public override UnitTag Name { get; protected set; } = "Mark.Burn";
    public BurnMark(IUnit owner) : base(owner, duration: 3f) { }
}

UnitCenter.Initialize();
var mark = MarkCenter.Create<BurnMark>(owner);
owner.Marks.AddMark(mark);
```

## 注意事项

- 仅扫描带 `FactoryAutoRegisterAttribute` 的类型。
- 构造函数首参数必须是 `IUnit owner`。
- 未注册类型会在 Create 时抛错。

## 相关跳转

- Unit: [../README.md](../README.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
