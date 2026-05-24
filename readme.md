# GoveKits

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Gove2004/GoveKits)

GoveKits 是一套 Unity 游戏开发框架，主打模块化、可扩展、工程化落地。

## 安装（重点）

安装请按下面顺序进行，不要跳步。

### 1. 先把依赖加入 Packages/manifest.json

将依赖清单（你项目中的 dependences.json 或 dependencies.json）合并到 Unity 项目的 Packages/manifest.json 的 dependencies 节点。

至少确保以下关键依赖存在：

```json
{
    "dependencies": {
        "com.tuyoogame.yooasset": "https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#2.3.18",
        "com.code-philosophy.hybridclr": "https://github.com/focus-creative-games/hybridclr_unity.git",
        "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.0",
    }
}
```

### 2. 再安装 GoveKits（Git 或 Dist）

方式 A：Git 安装（推荐）

1. 打开 Window -> Package Manager。
2. 点击左上角 +。
3. 选择 Add package from git URL。
4. 输入：

```text
https://github.com/Gove2004/GoveKits.git
```

方式 B：Dist 安装（本地包）

1. 准备 dist 目录中的包（或本地解压后的包目录）。
2. 打开 Window -> Package Manager。
3. 点击左上角 +。
4. 选择 Add package from disk。
5. 选择 dist 包内的 package.json。

也可以直接在 manifest.json 中写本地路径依赖（示例）：

```json
{
    "dependencies": {
        "com.gove.kits": "file:../dist/com.gove.kits"
    }
}
```

## 模块概览

| 模块 | 说明 | 文档 |
|---|---|---|
| Core | 基础能力（事件、日志、对象池、随机、场景、时间、单例） | [Runtime/Core/README.md](./Runtime/Core/README.md) |
| Storage | 资源与数据（YooAsset、配置、存档、热更、音频、本地化） | [Runtime/Storage/README.md](./Runtime/Storage/README.md) |
| Unit | 类 GAS 能力系统（属性、技能、Buff、Reaction、序列化） | [Runtime/Unit/README.md](./Runtime/Unit/README.md) |
| UI | UI 框架（MVVM、面板管理、自动收集） | Runtime/UI |
| AI | AI 框架（感知、记忆、思考、执行） | Runtime/AI |
| Architecture | 架构与业务组织相关能力 | Runtime/Architecture |
| Network | 网络相关能力模块 | Runtime/Network |
| Util | 通用工具与扩展工具集 | Runtime/Util |

## 快速开始

1. 先完成依赖注入和包安装。
2. 启动时优先初始化 Core 与 Storage。
3. 按业务选择接入 Unit / UI / AI 模块。

## 目录结构

```text
GoveKits/
├── Runtime/
│   ├── Core/
│   ├── Storage/
│   ├── Unit/
│   ├── UI/
│   ├── AI/
│   ├── Architecture/
│   ├── Network/
│   └── Util/
├── package.json
└── README.md
```

## 常见问题

| 问题 | 排查建议 |
|---|---|
| 安装后编译报缺包 | 先检查 manifest.json 是否已合并依赖，再 Reimport | 
| 资源系统无法工作 | 检查 YooAsset 依赖版本与初始化流程 | 
| 热更流程异常 | 检查 HybridCLR 依赖是否已正确安装 | 
| 模块命名空间无法识别 | 检查 asmdef 引用关系与脚本编译错误 | 

