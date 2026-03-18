# GoveKits 2.0

GoveKits 是一套面向 Unity 的运行时与编辑器工具集合，强调模块化、低耦合与高可维护性。

## 设计理念

- 模块独立: 每个子系统按职责拆分，避免“大一统管理器”。
- 统一规范: 接口命名、生命周期、调试入口、文档结构保持一致。
- 工程优先: 优先考虑团队协作、可调试性与长期演进成本。

## 架构介绍

项目主要分为三层:

1. Runtime
- Core: 通用基础设施（日志、单例、事件、对象池）
- Unit: 战斗与单位基础层（属性、标记、技能、反应、上下文、中心工厂）
- AI: 状态机等 AI 基础能力

2. Editor
- Core: Event / Pool 调试窗口
- Unit: Unit 运行时监控 Inspector

3. Plugins
- 第三方依赖与说明（如 UniTask、Newtonsoft 等）

## 快速开始

1. 克隆仓库到本地目录。
2. 在 Unity 中打开项目，或通过 Package Manager 以 from disk 方式引入 package.json。
3. 阅读 Runtime/Core 与 Runtime/Unit 文档，按最小示例接入。
4. 进入 Play 模式后用 Editor 调试窗口排查问题。

## 文档索引

- Runtime Core: [Assets/GoveKits/Runtime/Core/README.md](Assets/GoveKits/Runtime/Core/README.md)
- Runtime AI/FSM: [Assets/GoveKits/Runtime/AI/FSM/README.md](Assets/GoveKits/Runtime/AI/FSM/README.md)
- Runtime Unit: [Assets/GoveKits/Runtime/Unit/README.md](Assets/GoveKits/Runtime/Unit/README.md)
- Runtime Unit File Index: [Assets/GoveKits/Runtime/Unit/READ.md](Assets/GoveKits/Runtime/Unit/READ.md)
- Editor Core: [Assets/GoveKits/Editor/Core/README.md](Assets/GoveKits/Editor/Core/README.md)
- Editor Unit: [Assets/GoveKits/Editor/Unit/README.md](Assets/GoveKits/Editor/Unit/README.md)
- Plugins: [Assets/GoveKits/Plugins/README.md](Assets/GoveKits/Plugins/README.md)

## 相关跳转

- Core Event: [Assets/GoveKits/Runtime/Core/Event/README.md](Assets/GoveKits/Runtime/Core/Event/README.md)
- Core Pool: [Assets/GoveKits/Runtime/Core/Pool/README.md](Assets/GoveKits/Runtime/Core/Pool/README.md)
- Core Singleton: [Assets/GoveKits/Runtime/Core/Singleton/README.md](Assets/GoveKits/Runtime/Core/Singleton/README.md)
