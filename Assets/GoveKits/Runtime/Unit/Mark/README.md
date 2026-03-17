# Mark Module

Mark 用于给 Unit 挂载带时间语义的状态效果，例如：

- 中毒（周期掉血）
- 燃烧（持续伤害）
- 护盾（限时增益）

当前实现是纯 OnUpdate 驱动，不依赖外部 Timer 系统。

## 1. 核心结构

### 1.1 UnitMark

基础状态类型，负责：

- 生命周期：OnApply / OnStack / OnUpdate / OnRemove
- 堆叠：Stack 与 MaxStack
- 持续时间：Duration、Timer、IsExpired

关键规则：

- Duration > 0：按时间推进，超时后 IsExpired = true
- Duration <= 0：视为永久（不会自动过期）

### 1.2 TickMark

在 UnitMark 基础上增加周期触发能力：

- TickInterval：触发间隔
- OnTick()：每次间隔到达时回调

### 1.3 MarkContainer

容器负责：

- AddMark：新增或同名堆叠
- RemoveMark：主动移除
- UpdateMarks(deltaTime)：推进所有 Mark，并清理过期项
- HasTag：用于标签查询

## 2. 生命周期说明

1. 首次添加：调用 OnApply
2. 重复添加同名：调用 OnStack
3. 每帧驱动：调用 OnUpdate(deltaTime)
4. 标记过期后：由容器移除并调用 OnRemove

## 3. 快速开始

下面是一个最小的中毒示例。

```csharp
using UnityEngine;
using GoveKits.Runtime.Unit;

public sealed class DemoUnit : MonoBehaviour, IUnit
{
    public AttributeContainer Attributes { get; } = new AttributeContainer();
    public MarkContainer Marks { get; } = new MarkContainer();

    private void Update()
    {
        Marks.UpdateMarks(Time.deltaTime);
    }

    [ContextMenu("Apply Poison")]
    private void ApplyPoison()
    {
        Marks.AddMark(new DemoPoisonMark(this, damagePerTick: 3f, duration: 5f));
    }

    public void TakeDamage(float value)
    {
        Debug.Log($"TakeDamage: {value}");
    }
}

public sealed class DemoPoisonMark : TickMark
{
    private readonly float _damagePerTick;

    public override UnitTag Name => "Poison";

    public DemoPoisonMark(IUnit owner, float damagePerTick, float duration)
        : base(owner, interval: 1f, stack: 1, duration: duration)
    {
        _damagePerTick = damagePerTick;
        MaxStack = 5;
    }

    protected override void OnTick()
    {
        // 在此实现业务扣血逻辑。
        if (Owner is DemoUnit unit)
        {
            unit.TakeDamage(_damagePerTick * Stack);
        }
    }

    public override void OnStack(UnitMark newMark)
    {
        base.OnStack(newMark);
        // 可在此加入额外堆叠逻辑。
    }

    public override void OnRemove()
    {
        base.OnRemove();
        Debug.Log("Poison removed");
    }
}
```

## 4. 注意事项

- 由 Unit 或战斗系统统一驱动 Marks.UpdateMarks(deltaTime)。
- 尽量让 Name 全局唯一，避免不同语义共用同名标签。
- 如果某个 Mark 需要永久生效，Duration 设为 0 或负值。
- TickMark 建议用于固定频率逻辑，不要在 OnTick 中做过重操作。
