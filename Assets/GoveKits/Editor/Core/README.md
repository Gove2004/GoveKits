# Editor Core 开发文档

Editor/Core 提供运行时调试窗口，配套 Runtime Core 使用。

## 设计理念

- 让核心系统状态可视化。
- 降低排错成本。
- 运行期问题就地定位。

## 架构介绍

- EventDebuggerWindow.cs
- PoolDebuggerWindow.cs

## 快速开始

1. 进入 Play 模式。
2. 打开菜单:
   - GoveKits/Core/Event Debugger
   - GoveKits/Core/Pool Debugger
3. 根据过滤条件查看活跃通道与池状态。

```csharp
using UnityEditor;

// 也可通过命令方式直接打开窗口
EditorApplication.ExecuteMenuItem("GoveKits/Core/Event Debugger");
EditorApplication.ExecuteMenuItem("GoveKits/Core/Pool Debugger");
```

## 注意事项

- 非 Play 模式下数据可能为空。
- Auto Refresh 过高会增加编辑器开销。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Runtime Core: [../../Runtime/Core/README.md](../../Runtime/Core/README.md)
