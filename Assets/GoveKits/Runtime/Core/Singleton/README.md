# Singleton Module

GoveKits 单例模块提供两种基类：

- `CSharpSingleton<T>`：纯 C# 单例
- `MonoSingleton<T>`：Unity 组件单例

命名空间：`GoveKits.Runtime.Core.Singleton`

## 1. 文件说明

- `CSharpSingleton.cs`：线程安全的纯 C# 单例
- `MonoSingleton.cs`：场景对象单例，支持自动查找/创建与跨场景保留

## 2. 核心结构

### 2.1 CSharpSingleton<T>

适用场景：

- 配置管理器
- 纯逻辑服务
- 不依赖 Unity 生命周期的系统对象

特性：

- 延迟初始化（首次访问 `Instance` 时创建）
- 双重检查锁（DCL）保证线程安全
- 通过 `OnSingletonInit()` 提供初始化钩子

约束：

```csharp
where T : CSharpSingleton<T>, new()
```

### 2.2 MonoSingleton<T>

适用场景：

- 需要场景对象和 Unity 生命周期函数的全局管理器
- 需要跨场景保留的系统组件

特性：

- 首次访问时自动查找场景已有实例
- 若不存在则自动创建 `GameObject` 并挂载组件
- 自动执行 `DontDestroyOnLoad`
- 检测到多实例时输出日志提示
- 应用退出后再次访问 `Instance` 返回 `null`

约束：

```csharp
where T : MonoSingleton<T>
```

## 3. 快速开始

### 3.1 CSharpSingleton<T>

```csharp
using GoveKits.Runtime.Core.Singleton;

public class DemoGameConfig : CSharpSingleton<DemoGameConfig>
{
    public int MaxLevel;

    protected override void OnSingletonInit()
    {
        MaxLevel = 100;
    }
}

int maxLevel = DemoGameConfig.Instance.MaxLevel;
```

### 3.2 MonoSingleton<T>

```csharp
using UnityEngine;
using GoveKits.Runtime.Core.Singleton;

public class DemoAudioManager : MonoSingleton<DemoAudioManager>
{
    public void PlayClick()
    {
        Debug.Log("Play click");
    }
}

DemoAudioManager.Instance.PlayClick();
```

## 4. 注意事项

- 不依赖 Unity 生命周期时，优先使用 `CSharpSingleton<T>`。
- 需要 `MonoBehaviour` 生命周期时，使用 `MonoSingleton<T>`。
- `MonoSingleton<T>` 检测到多实例只会告警，不会自动销毁多余对象。
- 单例应保持职责单一，避免成为“万能管理器”。
