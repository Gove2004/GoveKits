# Times

高性能定时模块，基于时间轮（Timing Wheel）实现大量定时/循环任务的低开销调度。

## 组成
- `TimerManager`：管理两套时间轮（受缩放/真实时间），提供 `Once`/`Loop` API 并负责逐帧驱动。
- `TimeWheel`：底层时间轮结构，按固定 tick 推进并分发回调。
- `Timer`：定时句柄，支持暂停/恢复/取消与循环。
- `Timeline`：示例性（当前注释掉）轮式时间轴实现草案。

## 快速上手
```csharp
// 初始化（通常在 GoveKit.Awake 调用）
TimerManager.Initialize(0.05f);

// 一次性触发（1.5 秒后）
var t1 = TimerManager.Once(1.5f, () => Debug.Log("once"));

// 循环触发（每 2 秒，无限循环，使用真实时间）
var t2 = TimerManager.Loop(2f, () => Debug.Log("loop"), -1, useRealTime: true);

// 暂停/恢复
t2.Pause();
t2.Resume();

// 取消
t2.Cancel();

// MonoBehaviour.Update 中驱动
TimerManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
```

更多 API 与细节请参考源码 XML 注释。