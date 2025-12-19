# Singleton

单例模式实现：分别提供纯 C# 线程安全单例与 Unity MonoBehaviour 单例基类。

## 组成
- `Singleton<T>`：纯 C# 单例，双重检查锁定，线程安全，支持自定义初始化。
- `MonoSingleton<T>`：MonoBehaviour 单例，自动查找/创建实例，支持DontDestroyOnLoad 跨场景持久化。

## 用法

### C# 单例
```csharp
public class MyService : Singleton<MyService>
{
    protected override void SingletonInit()
    {
        // 初始化逻辑
    }
}

// 访问
var service = MyService.Instance;
```

### MonoBehaviour 单例
```csharp
public class GameManager : MonoSingleton<GameManager>
{
    // 自动处理：单例实例自动创建、查找、跨场景保留
}

// 访问
var manager = GameManager.Instance;
```

更多详见源码 XML 注释。