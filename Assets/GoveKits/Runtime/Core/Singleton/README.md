# Singleton

GoveKits 的单例模块提供两种基类：

- `CSharpSingleton<T>`：用于纯 C# 类（非 `MonoBehaviour`）
- `MonoSingleton<T>`：用于 Unity 组件类（继承 `MonoBehaviour`）

命名空间：`GoveKits.Core.Singleton`

## 文件说明

- `CSharpSingleton.cs`：线程安全的纯 C# 单例
- `MonoSingleton.cs`：Unity 场景对象单例，支持自动查找/创建与跨场景保留

## 1) CSharpSingleton<T>

适用场景：

- 配置管理器
- 纯逻辑服务
- 不依赖 Unity 生命周期的系统对象

特性：

- 延迟初始化（首次访问 `Instance` 时创建）
- 双重检查锁（DCL）保证线程安全
- 通过 `OnSingletonInit()` 提供初始化钩子

### 使用示例

```csharp
using GoveKits.Core.Singleton;

public class GameConfig : CSharpSingleton<GameConfig>
{
    public int MaxLevel;

    protected override void OnSingletonInit()
    {
        MaxLevel = 100;
    }
}

// 访问
int maxLevel = GameConfig.Instance.MaxLevel;
```

### 约束

`T` 必须满足：

```csharp
where T : CSharpSingleton<T>, new()
```

也就是：

- 必须继承 `CSharpSingleton<T>`
- 必须有无参构造函数

## 2) MonoSingleton<T>

适用场景：

- 需要挂在场景中的全局管理器
- 需要使用 Unity 生命周期函数（`Awake`/`Start`/`Update`）
- 需要跨场景保留对象

特性：

- 首次访问时自动查找场景中的已有实例
- 若不存在则自动创建 `GameObject` 并挂载组件
- 自动执行 `DontDestroyOnLoad`，跨场景保留
- 检测到多实例时输出日志提示
- 应用退出后再次访问 `Instance` 返回 `null`

### 使用示例

```csharp
using GoveKits.Core.Singleton;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    public void PlayClick()
    {
        Debug.Log("Play click");
    }
}

// 调用
AudioManager.Instance.PlayClick();
```

### 约束

`T` 必须满足：

```csharp
where T : MonoSingleton<T>
```

并且类型本身要继承 `MonoBehaviour`。

## 常见问题

### Q1: 什么时候用哪一种单例？

- 不依赖 Unity 对象生命周期：优先 `CSharpSingleton<T>`
- 需要场景对象与 Unity 生命周期：使用 `MonoSingleton<T>`

### Q2: `MonoSingleton<T>` 为什么在退出后返回 `null`？

为避免应用退出阶段重复创建“幽灵对象”，内部使用 `_applicationIsQuitting` 标记阻止重新初始化。

### Q3: 出现多个 `MonoSingleton` 实例会怎样？

当前实现会输出日志警告（不会自动销毁多余实例）。建议在项目逻辑中避免手动重复创建。

## 建议实践

- 单例只承载全局职责，避免成为“万能管理器”
- 纯数据/服务逻辑放在 `CSharpSingleton<T>`
- 与场景和组件强相关的逻辑放在 `MonoSingleton<T>`
- 保持单例可测试：尽量拆分业务逻辑到可注入的普通类
