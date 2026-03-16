
---

# GoveKits 2.0

> GoveKits 将直接覆写 1.X，迎来全新的 2.0 版本。

---

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Gove2004/GoveKits)

---

## 安装指南（From Disk）

本框架采用 Unity Package Manager 本地包管理模式：

1. **Clone 本仓库**：将代码克隆到你电脑的任意位置（例如 `E:\Unity Projects\GoveKits`）。
2. **打开项目**：进入你需要使用该框架的 Unity 工程。
3. **打开 Package Manager**：点击 `Window > Package Manager`。
4. **添加包**：
   - 点击左上角的 `+` 号。
   - 选择 `Add package from disk...`。
   - 导航到你克隆的 GoveKits 文件夹，选中 `package.json`。
5. **开始使用**：Unity 会自动识别并编译，现在你可以在代码中直接调用 GoveKits 了。

---

## 模块概览

### Runtime/Core

框架的基础运行时模块，命名空间统一为 `GoveKits.Runtime.Core`。

#### GoveKitsCore — 日志

统一日志入口，支持标签、消息、颜色和等级。

```csharp
GoveKitsCore.Log("MySystem", "初始化完成");
GoveKitsCore.Log("MySystem", "出现警告", logType: GoveKitsCore.LogType.Warning);
GoveKitsCore.Log("MySystem", "发生错误", logType: GoveKitsCore.LogType.Error);
```

---

#### Singleton — 单例

提供两种单例基类，详见 [`Runtime/Core/Singleton/README.md`](Assets/GoveKits/Runtime/Core/Singleton/README.md)。

| 基类 | 适用场景 |
|---|---|
| `CSharpSingleton<T>` | 纯 C# 类，线程安全，双重检查锁 |
| `MonoSingleton<T>` | MonoBehaviour，自动查找/创建，跨场景保留 |

```csharp
// 纯 C# 单例
public class GameManager : CSharpSingleton<GameManager>
{
    protected override void OnSingletonInit() { }
}

// MonoBehaviour 单例
public class AudioManager : MonoSingleton<AudioManager>
{
    protected override void OnSingletonInit() { }
}
```

---

#### Event — 事件系统

轻量、类型安全的事件总线，详见 [`Runtime/Core/Event/README.md`](Assets/GoveKits/Runtime/Core/Event/README.md)。

- 多总线路由（默认总线 `main`）
- 监听器优先级
- 事件传播中断（`IsBreak`）
- 事件对象池化复用（与 `PoolCore` 集成）

```csharp
// 定义事件
public class DamageEvent : EventInfo
{
    public float Amount;
}

// 订阅
EventCore.Subscribe<DamageEvent>(OnDamage);

// 发布
EventCore.Publish(new DamageEvent { Amount = 10f });

// 取消订阅
EventCore.Unsubscribe<DamageEvent>(OnDamage);
```

---

#### Pool — 对象池

统一的对象池系统，分为纯 C# 池和 GameObject 池，详见 [`Runtime/Core/Pool/README.md`](Assets/GoveKits/Runtime/Core/Pool/README.md)。

| 类型 | 适用场景 |
|---|---|
| `CSharpPool<T>` | 临时数据、结算对象、命令对象等 |
| `GameObjectPool` | 子弹、特效、敌人等场景对象 |

```csharp
// C# 对象池
PoolCore.Create<EnemyData>(count: 8, maxSize: 64);
EnemyData data = PoolCore.Get<EnemyData>();
PoolCore.Return(data);

// GameObject 池
PoolCore.Create(bulletPrefab, count: 16, maxSize: 64);
GameObject bullet = PoolCore.Get(bulletPrefab);
PoolCore.Return(bullet);
```

---

### Editor Tools

在 Unity 编辑器菜单 `GoveKits/Core` 下提供以下调试窗口，仅在运行时生效：

| 菜单项 | 功能 |
|---|---|
| `Event Debugger` | 查看当前所有总线的活跃频道与发布历史，支持 Bus 切换、事件类型过滤 |
| `Pool Debugger` | 查看 C# 池与 GameObject 池的缓存/活跃状态，支持警告阈值可视化（Warning / Danger 两级）|

---

## 目录结构

```
Assets/GoveKits/
├── Runtime/
│   └── Core/
│       ├── GoveKitsCore.cs      # 日志
│       ├── Singleton/           # 单例基类
│       ├── Event/               # 事件系统
│       └── Pool/                # 对象池
└── Editor/
    └── Core/
        ├── EventDebuggerWindow.cs   # 事件调试窗口
        └── PoolDebuggerWindow.cs    # 对象池调试窗口
```

---