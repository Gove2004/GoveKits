# Runtime AI FSM 开发文档

FSM 提供轻量状态机，支持异步进入/退出与双更新循环。

## 设计理念

- 状态职责最小化。
- 切换路径显式可追踪。
- 可在战斗、AI、流程机中复用。

## 架构介绍

- IFSMObject.cs: 宿主约束
- BaseState.cs: 状态抽象
- FSM.cs: 驱动与切换管理

## 快速开始

```csharp
public enum EnemyState { Idle, Chase }

public class EnemyAgent : MonoBehaviour, IFSMObject
{
    private FSM<EnemyState, EnemyAgent> _fsm;

    public void InitFSM()
    {
        _fsm.AddState(EnemyState.Idle, new IdleState());
        _fsm.AddState(EnemyState.Chase, new ChaseState());
    }

    private void Awake()
    {
        _fsm = new FSM<EnemyState, EnemyAgent>(this);
        _fsm.Start(EnemyState.Idle);
    }

    private void Update() => _fsm.Update();
}
```

## 注意事项

- 切换中再次切换会被忽略（不排队）。
- 异步 `OnEnter/OnExit` 需避免长阻塞。
- 宿主销毁时调用 `Dispose`。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Unit: [../../Unit/README.md](../../Unit/README.md)
