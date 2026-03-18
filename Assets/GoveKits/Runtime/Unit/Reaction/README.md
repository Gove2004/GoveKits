# Reaction Module

Reaction 用于给 Unit 挂载事件驱动的被动技能，例如：

- 受击后触发反击
- 受到治疗时追加护盾
- 击杀后叠加攻击增益

当前实现基于 EventCore 订阅机制，由容器统一管理激活与释放。

## 1. 核心结构

### 1.1 IUnitReaction

反应统一接口，约定：

- `Name`：反应唯一标识（UnitTag）
- `Owner`：归属 Unit
- `IsActive`：是否处于激活状态
- `Activate()` / `Deactivate()`：启停监听
- `Dispose()`：释放资源

### 1.2 UnitReaction<T>

反应基类，负责通用生命周期：

- 泛型事件类型：`T : EventInfo, new()`
- 激活时自动订阅 `EventCore.Subscribe<T>`
- 停用时自动反订阅
- 内置幂等保护：重复激活不会重复订阅，重复停用不会报错

### 1.3 DelegateReaction<T>

委托版反应，适合轻量逻辑：

- 构造时传入 `Action<T>` 即可完成响应定义
- 不需要额外派生类即可快速挂载一个被动

### 1.4 ReactionContainer

容器负责：

- `AddReaction`：新增反应（同名会先移除旧实例）
- `RemoveReaction`：移除并释放反应
- `SetActive(bool)`：批量激活/停用所有反应
- `ActivateReaction` / `DeactivateReaction`：单个反应启停
- `Clear`：清空并释放全部反应

## 2. 生命周期说明

1. 添加反应：`AddReaction(reaction)`
2. 容器激活：`SetActive(true)`
3. 事件触发：`OnEvent -> OnReaction`
4. 容器停用：`SetActive(false)`
5. 移除或清空：`RemoveReaction / Clear`（内部自动 Dispose）

## 3. 快速开始

下面是一个最小的“受击反击”示例。

```csharp
using UnityEngine;
using GoveKits.Runtime.Core.Event;
using GoveKits.Runtime.Unit;

public sealed class DamageEvent : EventInfo
{
    public IUnit Target;
    public float Value;
}

public sealed class DemoUnit : MonoBehaviour, IUnit
{
    public AttributeContainer Attributes { get; } = new AttributeContainer();
    public ReactionContainer Reactions { get; } = new ReactionContainer();

    private void Awake()
    {
        // 使用委托快速创建一个被动：当自己受击时输出日志
        var reaction = new DelegateReaction<DamageEvent>(
            owner: this,
            name: "CounterAttack",
            reactionAction: OnDamaged,
            priority: 10);

        Reactions.AddReaction(reaction);
        Reactions.SetActive(true);
    }

    [ContextMenu("Simulate Damage")]
    private void SimulateDamage()
    {
        EventCore.Publish<DamageEvent>(e =>
        {
            e.Target = this;
            e.Value = 20f;
        });
    }

    private void OnDamaged(DamageEvent e)
    {
        // 只处理自己受到的伤害
        if (!ReferenceEquals(e.Target, this))
        {
            return;
        }

        Debug.Log($"CounterAttack triggered, damage = {e.Value}");
    }

    private void OnDestroy()
    {
        Reactions.Clear();
    }
}
```

## 4. 注意事项

- 反应默认不会自动激活，添加后请调用 `SetActive(true)` 或 `ActivateReaction`。
- 反应标签 `Name` 建议全局唯一，避免同名覆盖造成误替换。
- 如果需要更强类型安全，可优先使用继承 `UnitReaction<T>` 的方式封装业务逻辑。
- 建议在 Unit 销毁时调用 `Clear()`，确保订阅句柄及时释放。
