# Editor Core Module

Editor/Core 提供运行时调试窗口，帮助快速定位 Event 与 Pool 相关问题。

## 设计理念

- 运行时可观测: 将关键系统状态可视化。
- 低侵入调试: 不改业务代码即可查看系统运行数据。
- 快速反馈: 自动刷新与筛选能力支持高频排错。

## 架构介绍

- EventDebuggerWindow.cs
  - 展示总线、频道订阅和发布历史
- PoolDebuggerWindow.cs
  - 展示 CSharp 池与 GameObject 池状态
  - 支持阈值告警显示

## 快速开始

1. 进入 Play 模式。
2. 菜单打开 GoveKits/Core/Event Debugger 或 Pool Debugger。
3. 按模块筛选查看实时状态。
4. 结合 Runtime 文档定位调用链问题。

## 相关跳转

- Root: [../../../../README.md](../../../../README.md)
- Runtime Core: [../../Runtime/Core/README.md](../../Runtime/Core/README.md)
- Event: [../../Runtime/Core/Event/README.md](../../Runtime/Core/Event/README.md)
- Pool: [../../Runtime/Core/Pool/README.md](../../Runtime/Core/Pool/README.md)
