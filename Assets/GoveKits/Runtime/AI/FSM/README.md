# FSM Module

轻量泛型有限状态机，面向 Unity 运行时逻辑，支持异步状态进入/退出和双更新循环。

## 1. 概览

- 使用枚举作为状态标签
- 状态切换支持异步（`OnEnter` / `OnExit`）
- 同时支持 `Update` 与 `FixedUpdate`
- 支持在 Owner 中通过 `InitFSM()` 完成状态注册
- 提供 `Dispose()` 统一清理状态资源

## 2. 核心类型

- `IFSMObject`
  - FSM 持有者接口，提供 `InitFSM()` 初始化入口。
- `BaseState<TStateEnum, TFSMObject>`
  - 状态基类，提供 `OnEnter` / `OnUpdate` / `OnFixedUpdate` / `OnExit`。
  - 内置 `ChangeState(next)` 便捷切换。
  - 提供 `Dispose()` 用于释放状态内部资源。
- `FSM<TStateEnum, TFSMObject>`
  - 负责状态注册、启动、切换、更新驱动和统一清理。

## 3. 运行流程

### 3.1 Start 流程

`Start(initialState)` 的执行顺序：

1. 首次启动时执行 `Owner.InitFSM()`（仅一次）
2. 校验 `initialState` 是否已注册
3. 设为当前状态并调用 `OnEnter().Forget()`

### 3.2 ChangeState 流程

`ChangeState(target)` 的执行顺序：

1. 当前状态 `OnExit()`
2. 更新 `Current`
3. 目标状态 `OnEnter()`

切换保护策略：

- 若正在切换（`_isTransitioning == true`），新的切换请求会被直接忽略
- 若目标状态不存在，直接返回
- 若目标状态等于当前状态，直接返回

## 4. 快速开始

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using GoveKits.Runtime.AI.FSM;

public enum DemoEnemyState
{
    Idle,
    Chase,
}

public class DemoEnemyAgent : MonoBehaviour, IFSMObject
{
    private FSM<DemoEnemyState, DemoEnemyAgent> _fsm;

    public Transform Target;
    public float MoveSpeed = 3f;

    private void Awake()
    {
        _fsm = new FSM<DemoEnemyState, DemoEnemyAgent>(this);
        _fsm.Start(DemoEnemyState.Idle);
    }

    public void InitFSM()
    {
        // 在此注册状态。
        _fsm.AddState(DemoEnemyState.Idle, new DemoIdleState());
        _fsm.AddState(DemoEnemyState.Chase, new DemoChaseState());
    }

    private void Update()
    {
        _fsm.Update();
    }

    private void FixedUpdate()
    {
        _fsm.FixedUpdate();
    }

    private void OnDestroy()
    {
        _fsm?.Dispose();
    }
}

public sealed class DemoIdleState : BaseState<DemoEnemyState, DemoEnemyAgent>
{
    private float _timer;

    public override UniTask OnEnter()
    {
        _timer = 0f;
        return UniTask.CompletedTask;
    }

    public override void OnUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer >= 2f)
        {
            ChangeState(DemoEnemyState.Chase);
        }
    }
}

public sealed class DemoChaseState : BaseState<DemoEnemyState, DemoEnemyAgent>
{
    public override void OnUpdate()
    {
        if (Owner.Target == null)
        {
            ChangeState(DemoEnemyState.Idle);
            return;
        }

        var dir = (Owner.Target.position - Owner.transform.position).normalized;
        Owner.transform.position += dir * Owner.MoveSpeed * Time.deltaTime;
    }

    public override void Dispose()
    {
        // 在此释放事件订阅、定时器等状态资源。
        base.Dispose();
    }
}
```

## 5. 资源管理

- `FSM.Dispose()` 会：
  - 对当前状态调用 `OnExit().Forget()`
  - 对所有状态调用 `Dispose()`
  - 清空状态表并断开 Owner 引用
- 建议在 Owner 的 `OnDestroy` 中调用 `Dispose()`。

## 6. 注意事项

以下是当前实现的既定行为（不是 bug）：

- `Start` 的初次进入是 fire-and-forget（不等待 `OnEnter` 完成）
- `BaseState.ChangeState` 是 fire-and-forget 快捷方法
- 切换中收到的新切换请求会被丢弃，不排队

如果你需要“严格串行 + 可等待 + 请求排队”的切换语义，建议在现有 FSM 之上增加队列层或改造为 `ChangeStateAsync` 统一入口。
