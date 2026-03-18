# Runtime Unit Mark 开发文档

Mark 模块用于实现可叠层、可计时、可周期触发的状态。

## 设计理念

- 状态生命周期标准化。
- 叠层语义明确。
- 容器统一更新与回收。

## 架构介绍

- UnitMark.cs
- TickMark.cs（位于 UnitMark.cs）
- MarkContainer.cs

## 快速开始

```csharp
public sealed class PoisonMark : TickMark
{
    public override UnitTag Name { get; protected set; } = "Mark.Poison";

    public PoisonMark(IUnit owner) : base(owner, interval: 1f, duration: 5f)
    {
    }

    protected override void OnTick()
    {
        // 每秒效果
    }
}

owner.Marks.AddMark(new PoisonMark(owner));
```

## 注意事项

- `Duration <= 0` 代表永久。
- 同名 Mark 触发 OnStack，不会新建第二个实例。
- 记得每帧调用 `UpdateMarks(deltaTime)`。

## 相关跳转

- Unit: [../README.md](../README.md)
- Extension: [../Extension/README.md](../Extension/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
