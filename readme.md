# GoveKits

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Gove2004/GoveKits)

GoveKits 是一套面向 Unity 游戏的模块化开发框架，提供 Core 基础设施、Storage 存储与集成、Unit 数值能力系统、UI 面板框架、Network 网络协议以及 Editor 调试工具链。框架以静态 `*Core` 门面模式组织所有子系统，由 `GoveCore` 统一管理生命周期。

## 安装

安装请按以下顺序进行，不要跳步。

### 前置依赖

在 Unity 项目的 `Packages/manifest.json` 的 `dependencies` 节点中合并以下依赖：

```json
{
    "dependencies": {
        "com.tuyoogame.yooasset": "https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#2.3.18",
        "com.code-philosophy.hybridclr": "https://github.com/focus-creative-games/hybridclr_unity.git",
        "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.0",
        "com.unity.inputsystem": "1.7.0",
        "com.unity.render-pipelines.universal": "14.0.10"
    }
}
```

至少确保 `YooAsset`、`UniTask` 和 `HybridCLR` 存在，它们是 ResCore 和异步流程的基础。

### 方式 A：Git 安装（推荐）

1. 打开 Unity 编辑器 → Window → Package Manager。
2. 点击左上角 **+** 按钮。
3. 选择 **Add package from git URL**。
4. 输入：

```
https://github.com/Gove2004/GoveKits.git
```

5. 点击 Add，等待 Package Manager 拉取并编译。

### 方式 B：Dist 安装（本地包）

1. 准备 `dist` 目录中的包或本地解压后的包目录。
2. 打开 Package Manager → **+** → **Add package from disk**。
3. 选择包内 `package.json` 所在目录。

也可以在 `manifest.json` 中直接写本地路径：

```json
{
    "dependencies": {
        "com.gove.kits": "file:../your-local-govekits-path"
    }
}
```

### 安装后

1. 重新编译项目（Reimport）。
2. 在初始化场景中调用 `GoveCore.Setup()` 初始化。
3. 参考下方各模块文档进行具体功能接入。

## 模块概览

| 模块 | 说明 | 详细文档 |
|------|------|---------|
| **Core** | 基础能力：日志、对象池、随机数(RNG)、事件总线、时间轮、实体生成 | [Runtime/Core/README.md](./Runtime/Core/README.md) |
| **Storage** | 存储集成：资源加载(YooAsset)、配置表、存档、热更新(HybridCLR)、音频、多语言 | [Runtime/Storage/README.md](./Runtime/Storage/README.md) |
| **Unit** | 类 GAS 能力系统：属性、技能、标记(Buff/Debuff)、反应链、效果、序列化 | [Runtime/Unit/README.md](./Runtime/Unit/README.md) |
| **UI** | MVVM 面板框架：ViewModel 绑定、ViewPanel、自动组件收集 | [Runtime/UI/README.md](./Runtime/UI/README.md) |
| **Network** | 网络协议栈：HTTP、TCP/UDP、帧同步、状态同步、MessagePack 序列化 | [Runtime/Network/README.md](./Runtime/Network/README.md) |
| **Window** | Editor 调试工具链：15 个可视化窗口，覆盖 Core/Storage/Network/Unit 实时监控 | [Editor/README.md](./Editor/README.md) |

## 快速开始

1. 完成前置依赖和 GoveKits 包安装。
2. 在初始化场景中调用 `GoveCore.Setup()`。
3. 优先初始化 Core 与 Storage（`GoveCore.Setup()` → `ResCore.InitPackageAsync()` → `ConfigCore.Setup()`）。
4. 按需接入 Unit / UI / Network 模块。

## 目录结构

```
GoveKits/
├── Runtime/                  # 运行时框架代码
│   ├── Core/                 #   基础子系统（日志、池、事件、时间轮等）
│   ├── Storage/              #   存储与集成（资源、配置、存档、热更、音频、本地化）
│   ├── Unit/                 #   类 GAS 能力系统（属性、技能、标记、反应）
│   ├── UI/                   #   MVVM 面板框架
│   ├── Network/              #   网络协议栈（HTTP、TCP/UDP、帧同步）
│   ├── AI/                   #   AI 行为引擎（感知-记忆-思考循环）
│   ├── Architecture/         #   架构与业务组织
│   │   └── ECS/              #     轻量 ECS 架构原型
│   ├── Todo/                 #   实验性代码
│   └── Util/                 #   通用工具（单例、贝塞尔曲线、RNG）
├── Editor/                   # Unity Editor 调试窗口
├── package.json              # 包元数据
└── README.md                 # 本文件
```

## 常见问题

| 问题 | 排查建议 |
|------|---------|
| 安装后编译报缺包 | 先检查 `manifest.json` 是否已合并依赖，再 Reimport |
| 资源系统无法工作 | 检查 YooAsset 依赖版本与初始化流程 |
| 热更流程异常 | 检查 HybridCLR 依赖是否已正确安装 |
| 模块命名空间无法识别 | 检查 asmdef 引用关系与脚本编译错误 |
