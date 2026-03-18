# Runtime Unit Mark Module

Mark 模块用于描述持续状态、层数与周期触发效果。

## 设计理念

- 生命周期明确: Apply/Stack/Update/Remove 分阶段处理。
- 可叠层: 通过 Stack/MaxStack 管理叠加。
- 可计时: Duration/Timer/IsExpired 驱动过期清理。

## 架构介绍

- UnitMark.cs
  - UnitMark: 通用状态基类
  - TickMark: 周期触发状态基类
- MarkContainer.cs
  - 添加、移除、查询与统一更新

## 快速开始

1. 继承 UnitMark 或 TickMark 定义状态。
2. 约定唯一 Name 标签。
3. 通过 MarkContainer.AddMark 挂载。
4. 每帧调用 UpdateMarks 推进计时并自动移除过期状态。

## 相关跳转

- Unit: [../README.md](../README.md)
- Unit File Index: [../READ.md](../READ.md)
- Extension: [../Extension/README.md](../Extension/README.md)
- Reaction: [../Reaction/README.md](../Reaction/README.md)
