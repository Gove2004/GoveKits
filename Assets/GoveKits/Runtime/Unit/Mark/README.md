# Runtime Unit Mark 开发手册

Mark 是持续状态系统，用于中毒、灼烧、增益、冷却等具有生命周期的效果。

## 设计理念

- 状态统一建模，避免散落计时器。
- 同名状态有明确叠层语义。
- 更新与回收由容器负责。

## 架构介绍

- UnitMark: 基础状态
- TickMark: 周期触发状态
- MarkContainer: 状态管理与更新

## 快速开始

### 1. 定义周期伤害状态

```csharp
using GoveKits.Runtime.Unit;

public sealed class PoisonMark : TickMark
{
    public override UnitTag Name { get; protected set; } = "Mark.Poison";

    public PoisonMark(IUnit owner) : base(owner, interval: 1f, duration: 5f) { }

    protected override void OnTick()
    {
        Owner.Attributes.Change("Attr.Hp", -3f);
    }
}
```

### 2. 应用并刷新状态

```csharp
using GoveKits.Runtime.Unit;

public static class MarkUseCase
{
    public static void Apply(IUnit unit)
    {
        unit.Marks.AddMark(new PoisonMark(unit));
        unit.Marks.UpdateMarks(1f / 60f);
    }
}
```

## 注意事项

- `Duration <= 0` 表示常驻状态。
- 同名状态不会重复创建，通常会进入叠层逻辑。
- 自定义 Mark 如持有外部资源，记得在结束时释放。

## 常见故障排查

- 现象: Mark 看起来“加上了”但不生效。
    - 排查: 检查 `OnTick`/`OnApply` 是否真正修改了目标状态。
- 现象: Mark 永远不消失。
    - 排查: 检查 Duration 设置与 `UpdateMarks(deltaTime)` 调用频率。
- 现象: 叠层行为异常。
    - 排查: 检查同名 Tag 是否一致，以及叠层逻辑是否被覆盖实现。

## 相关跳转

- Unit: [../README.md](../README.md)
- Extension: [../Extension/README.md](../Extension/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



