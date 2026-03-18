# Editor Unit 开发文档

Editor/Unit 提供 UnitBehaviour 的运行时监控 Inspector。

## 设计理念

- 关键战斗数据一屏查看。
- 调试行为尽量不侵入业务代码。
- 与 Runtime Unit 数据结构一致。

## 架构介绍

- UnitBehaviourEditor.cs
  - Attributes 面板
  - Marks 面板
  - Abilities 面板
  - Reactions 面板

## 快速开始

1. 让角色组件继承 UnitBehaviour。
2. 进入 Play 模式并选中对象。
3. 在 Inspector 实时查看 Unit 数据。

```csharp
using GoveKits.Runtime.Unit;

public sealed class DemoUnit : UnitBehaviour
{
  public override void InitAttributes() { }
  public override void InitMarks() { }
  public override void InitAbilitys() { }
  public override void InitReactions() { }
}
```

## 注意事项

- 仅运行时显示完整调试数据。
- 标签命名建议统一，便于过滤检索。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Runtime Unit: [../../Runtime/Unit/README.md](../../Runtime/Unit/README.md)
