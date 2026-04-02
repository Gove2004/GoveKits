# GoveKits

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Gove2004/GoveKits)

GoveKits 是 Unity 游戏开发框架，提供核心系统、UI 框架、AI 系统等常用模块。采用组件化设计，开箱即用，帮助开发者快速搭建游戏架构。

## 简介

GoveKits 是一套轻量级、模块化的 Unity 游戏开发框架，核心设计理念：

- **组件化**：各模块独立，可按需选用
- **开箱即用**：最小配置即可运行
- **扩展性强**：接口设计支持自定义实现
- **性能友好**：避免不必要的 GC 和性能开销

### 核心特性

| 特性 | 说明 |
|---|---|
| 模块化 | Core、UI、AI 等模块独立，按需引用 |
| 静态入口 | 核心系统采用静态类，全局可访问 |
| 生命周期管理 | 统一的 Init/UnInit 生命周期接口 |
| 事件驱动 | 发布 - 订阅模式的事件系统 |
| 对象池 | 减少运行时 GC 分配 |
| MVVM | UI 数据绑定，分离视图与逻辑 |
| 组件化 AI | 感知 - 记忆 - 思考 - 执行闭环 |

## 安装

### 方式一：从 Git URL 安装（推荐）

1. 打开 Unity Package Manager（Window → Package Manager）
2. 点击左上角 [+] 按钮
3. 选择 `Add package from git URL`
4. 输入以下 URL 并点击 Add：

```
https://github.com/Gove2004/GoveKits.git
```

5. 等待下载完成，模块将自动导入项目

### 方式二：从磁盘安装

1. 克隆或下载 GoveKits 仓库到本地：

```bash
git clone https://github.com/Gove2004/GoveKits.git
```

2. 打开 Unity Package Manager
3. 点击左上角 [+] 按钮
4. 选择 `Add package from disk`
5. 选择 GoveKits 目录下的 `package.json` 文件
6. 模块将导入项目

### 方式三：手动复制

1. 克隆或下载 GoveKits 仓库
2. 将 `Runtime` 目录复制到 Unity 项目的 `Assets` 目录下
3. 在代码中通过 `GoveKits.Runtime` 命名空间访问

## 模块

GoveKits 目前包含以下核心模块：

| 模块 | 说明 | 文档 |
|---|---|---|
| **Core** | 核心基础库，提供事件、日志、对象池、随机、单例等通用系统 | [Core readme](./Core/readme.md) |
| **UI** | UI 框架，采用 MVVM 架构，提供面板管理、数据绑定、自动 UI 收集 | [UI readme](./UI/readme.md) |
| **AI** | AI 框架，采用感知 - 记忆 - 思考 - 执行闭环，支持 FSM、行为树等 | [AI readme](./AI/readme.md) |

### 模块依赖

```
┌─────────────────────────────────────────┐
│                  Core                   │
│  （基础模块，其他模块都依赖 Core）        │
└─────────────────────────────────────────┘
           │                    │
           ▼                    ▼
┌─────────────────┐    ┌─────────────────┐
│       UI        │    │       AI        │
│  （依赖 Core）   │    │  （依赖 Core）  │
└─────────────────┘    └─────────────────┘
```

### 模块说明

#### Core 核心模块

提供游戏开发常用基础系统：

- **Event**：发布 - 订阅模式事件总线
- **Log**：统一日志管理，支持多输出目标
- **Pool**：对象池系统，减少 GC 分配
- **Random**：确定性随机数生成
- **Singleton**：单例模式基类

详见：[Core/readme.md](./Core/readme.md)

#### UI 模块

提供 MVVM 架构的 UI 开发框架：

- **UIController**：面板管理与导航
- **VMContainer**：ViewModel 单例容器
- **ViewPanel**：面板基类，支持数据绑定
- **UIElementCollection**：自动 UI 组件收集

详见：[UI/readme.md](./UI/readme.md)

#### AI 模块

提供组件化 AI 开发框架：

- **AIActor**：AI 行动者基类
- **IAIMemory**：记忆接口
- **IAIObserver**：感知器接口
- **IAITinker**：思考者接口（支持 FSM、行为树等）

详见：[AI/readme.md](./AI/readme.md)

## 项目结构

```
GoveKits/
├── Runtime/
│   ├── Core/           # 核心模块
│   │   ├── Event/
│   │   ├── Log/
│   │   ├── Pool/
│   │   ├── Random/
│   │   └── Singleton/
│   ├── UI/             # UI 模块
│   └── AI/             # AI 模块
├── package.json
└── README.md
```

## 常见问题

| 问题 | 解决方案 |
|---|---|
| 模块导入后找不到命名空间 | 检查 Assembly Definition 配置 |
| UI 面板不显示 | 检查 UIController 是否正确配置 panelsArray |
| AI 不执行动作 | 检查 SetupAI 是否正确装配组件 |
| 事件不触发 | 检查是否正确订阅和发布事件 |
| 对象池不生效 | 检查对象是否实现 IPoolable 接口 |

