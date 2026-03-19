# Editor Unit 开发手册

Editor/Unit 提供 UnitBehaviour 的运行时 Inspector 监控，用于快速定位技能、状态、属性和被动行为问题。

## 设计理念

- 调试信息集中在 Inspector，减少日志轰炸。
- 不侵入业务逻辑，默认可直接复用。
- 面板结构对应 Runtime Unit 容器结构。

## 架构介绍

- UnitBehaviourEditor
  - Attributes 展示
  - Marks 展示
  - Abilities 展示
  - Reactions 展示

## 快速开始

### 1. 创建可观察 Unit

```csharp
using GoveKits.Runtime.Unit;

public sealed class DebugUnit : UnitBehaviour
{
    public override void InitAttributes()
    {
        var maxHp = Attributes.AddState("Attr.MaxHp", 100f);
        Attributes.AddRuntime("Attr.Hp", maxHp);
    }

    public override void InitMarks() { }
    public override void InitAbilities() { }
    public override void InitReactions() { }
}
```

### 2. 在运行期制造状态变化

```csharp
using UnityEngine;

public partial class DebugUnit
{
    [ContextMenu("Damage 20")]
    private void Damage() => Attributes.Change("Attr.Hp", -20f);
}
```

## 注意事项

- 仅 Play 模式能看到完整运行时数据。
- 过滤检索依赖标签规范，命名要统一。
- 面板显示是瞬时状态，调试时建议配合事件窗口。

## 常见故障排查

- 现象: Inspector 没有显示 Unit 调试区块。
    - 排查: 组件是否继承 `UnitBehaviour`，以及是否进入 Play 模式。
- 现象: 属性值变化但面板不变。
    - 排查: 检查属性是否通过容器 API 修改，而不是绕过容器直接改值。
- 现象: Abilities/Reactions 列表为空。
    - 排查: 检查 `InitAbilities` 与 `InitReactions` 是否真正注册实例。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Runtime Unit: [../../Runtime/Unit/README.md](../../Runtime/Unit/README.md)
- 术语与命名规范: [../../../../TERMINOLOGY.md](../../../../TERMINOLOGY.md)



