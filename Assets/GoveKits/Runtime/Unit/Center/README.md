# Runtime Unit Center 开发手册

Center 模块负责 Unit 子系统对象的集中注册与创建，避免业务层散乱反射或硬编码 `new`。

## 设计理念

- 注册集中化，构造行为标准化。
- 注解驱动扫描，减少人工同步成本。
- 初始化时发现问题，运行时稳定使用。

## 架构介绍

- FactoryAutoRegisterAttribute: 声明可自动注册类型
- AbilityCenter / MarkCenter / ReactionCenter: 分域工厂
- UnitCenter.Initialize: 一次性初始化入口

## 快速开始

### 1. 声明可注册类型

```csharp
using GoveKits.Runtime.Unit;

[FactoryAutoRegister]
public sealed class BurnMark : UnitMark
{
    public override UnitTag Name { get; protected set; } = "Mark.Burn";
    public BurnMark(IUnit owner) : base(owner, duration: 3f) { }
}
```

### 2. 初始化并创建实例

```csharp
using GoveKits.Runtime.Unit;

public static class CenterUseCase
{
    public static void Install(IUnit owner)
    {
        UnitCenter.Initialize();
        var burn = MarkCenter.Create<BurnMark>(owner);
        owner.Marks.AddMark(burn);
    }
}
```

## 注意事项

- 只有标注了 `FactoryAutoRegister` 的类型会被纳入自动注册。
- 可创建类型构造函数必须满足中心约定。
- 初始化失败优先检查命名空间冲突与构造签名。

## 常见故障排查

- 现象: `Create<T>` 抛类型未注册异常。
    - 排查: 类型是否加了 `FactoryAutoRegister`，并且初始化是否已执行。
- 现象: 注册成功但构造失败。
    - 排查: 检查构造签名是否满足中心约定（如 `IUnit owner`）。
- 现象: 同类型表现不一致。
    - 排查: 检查是否存在同名类型或多程序集重复定义。

## 相关跳转

- Unit: [../README.md](../README.md)
- Ability: [../Ability/README.md](../Ability/README.md)
- Mark: [../Mark/README.md](../Mark/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



