# Runtime AI FSM 开发手册

FSM 模块提供轻量状态机框架，用于 AI、流程控制和阶段驱动逻辑。

## 设计理念

- 状态职责单一，避免“大状态类”。
- 切换路径可读可跟踪。
- 支持异步进入/退出，适配动画和加载流程。

## 架构介绍

- IFSMObject: 状态机宿主约束
- BaseState: 状态行为抽象
- FSM: 状态注册、启动、切换与更新

## 快速开始

### 1. 定义状态与宿主

```csharp
using GoveKits.Runtime.AI.FSM;
using UnityEngine;

public enum EnemyState { Idle, Chase }

public sealed class EnemyAgent : MonoBehaviour, IFSMObject
{
    private FSM<EnemyState, EnemyAgent> _fsm;

    public void InitFSM()
    {
        _fsm.AddState(EnemyState.Idle, new IdleState());
        _fsm.AddState(EnemyState.Chase, new ChaseState());
    }
}
```

### 2. 启动与驱动更新

```csharp
private void Awake()
{
    _fsm = new FSM<EnemyState, EnemyAgent>(this);
    InitFSM();
    _fsm.Start(EnemyState.Idle);
}

private void Update() => _fsm.Update();
```

## 注意事项

- 切换过程中再次切换通常会被忽略。
- 异步 `OnEnter/OnExit` 不要长时间阻塞主流程。
- 宿主销毁时请调用 `Dispose` 回收状态资源。

## 常见故障排查

- 现象: 状态机一直停在初始状态。
    - 排查: 检查是否调用了 `Start`，以及 `Update` 是否每帧驱动。
- 现象: 状态切换请求无效。
    - 排查: 检查目标状态是否已注册、切换时机是否处于禁止切换阶段。
- 现象: 离开状态逻辑未执行。
    - 排查: 检查 `OnExit` 是否被覆盖并正确等待异步流程。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Unit: [../../Unit/README.md](../../Unit/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)



