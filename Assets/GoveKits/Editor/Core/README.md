# Editor Core 开发手册

Editor/Core 提供 Runtime/Core 的调试窗口，帮助定位事件流和池状态问题。

## 设计理念

- 运行期问题可视化，而不是只看日志。
- 调试窗口使用门槛低，接入零侵入。
- 与 Runtime 同步演进，保证观察维度一致。

## 架构介绍

- EventDebuggerWindow: 事件通道与监听观察
- PoolDebuggerWindow: 池容量与活跃对象观察

## 快速开始

### 1. 通过菜单打开窗口

```csharp
using UnityEditor;

EditorApplication.ExecuteMenuItem("GoveKits/Core/Event Debugger");
EditorApplication.ExecuteMenuItem("GoveKits/Core/Pool Debugger");
```

### 2. 添加开发快捷菜单

```csharp
using UnityEditor;

public static class EditorCoreShortcut
{
    [MenuItem("GoveKits/Dev/Open Event Debugger")]
    public static void OpenEvent() => EditorApplication.ExecuteMenuItem("GoveKits/Core/Event Debugger");
}
```

## 注意事项

- 绝大多数数据只有 Play 模式可见。
- 过高刷新频率会增加 Editor 开销。
- 窗口信息是观察值，不会回写 Runtime。

## 常见故障排查

- 现象: 调试窗口打开后为空。
    - 排查: 确认当前是否处于 Play 模式，且 Runtime 系统已初始化。
- 现象: 数据刷新明显滞后。
    - 排查: 检查窗口刷新策略和项目帧率是否过低。
- 现象: 事件或池状态显示不全。
    - 排查: 检查是否使用了不同总线或对象未经过 PoolCore。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Runtime Core: [../../Runtime/Core/README.md](../../Runtime/Core/README.md)
- 术语与命名规范: [../../../../TERMINOLOGY.md](../../../../TERMINOLOGY.md)



