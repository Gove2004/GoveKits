# Config 模块文档

## 文档速览
- 目标: 通过注解声明配置文件位置与解析类型，启动时一次性加载到内存。
- 核心入口: `ConfigCore.InitAsync`、`ConfigCore.Load<T>(predicate)`、`ConfigCore.LoadAll<T>()`。
- 适用场景: 数值配置、敌人参数、掉落表、关卡参数等静态数据。

## 阅读路径
1. 先看 `ConfigAttribute.cs` 了解如何标注配置类型。
2. 再看 `ConfigBindingScanner.cs` 了解类型扫描规则。
3. 最后看 `ConfigCore.cs` 了解加载与查询流程。
4. 解析细节见 `Parser/JsonConfigParser.cs` 与 `Parser/CsvConfigParser.cs`。

## 设计理念
- 类型绑定配置: 使用 `[Config(...)]` 将数据类型与文件路径直接绑定。
- 启动预热加载: 启动时加载全部配置，运行时只做内存查询。
- 解析策略解耦: JSON/CSV 解析器通过 `IConfigParser` 统一接入。
- API 简洁: 外部只暴露 Init + Load 系列，减少调用方认知负担。

## 架构介绍
- `ConfigAttribute`: 描述文件路径、来源（Resources/StreamingAssets）、格式（Json/Csv）。
- `ConfigBindingScanner`: 扫描当前域中所有带注解且实现 `IConfigData` 的类型。
- `ConfigCore`: 负责加载、解析、缓存、查询。
- `IConfigParser`: 解析器抽象。
- `JsonConfigParser` / `CsvConfigParser`: 默认实现。

## 快速开始
### 1) 定义配置类型并标注来源
```csharp
using GoveKits.Runtime.Storage.Config;

[Config("Configs/Enemy", ConfigFileType.Json, ConfigSourceType.Resources)]
public class EnemyConfig : IConfigData
{
    public int Id;
    public string Name;
    public float Hp;
}
```

### 2) 启动时初始化
```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Config;

public static class GameBootstrap
{
    public static async UniTask InitAsync()
    {
        await ConfigCore.InitAsync();
    }
}
```

### 3) 运行时查询
```csharp
using System.Collections.Generic;
using GoveKits.Runtime.Storage.Config;

List<EnemyConfig> all = ConfigCore.LoadAll<EnemyConfig>();
List<EnemyConfig> boss = ConfigCore.Load<EnemyConfig>(x => x.Hp >= 5000f);
```

## 注意事项
- `InitAsync` 调用前不能使用 `Load` 系列，否则会抛出初始化异常。
- `Resources` 来源路径可不带扩展名；`StreamingAssets` 建议带扩展名。
- JSON 解析器支持 `List<T>`、`Dictionary<int,T>`、`Dictionary<string,T>`、单对象 `T`。
- CSV 首行必须是表头，字段按名称（忽略大小写）映射到字段/属性。
- Android/WebGL 下 StreamingAssets 可能走 URI，内部会自动切到 `UnityWebRequest`。

## 相关跳转
- `ConfigCore.cs`
- `ConfigAttribute.cs`
- `ConfigBindingScanner.cs`
- `Parser/IConfigParser.cs`
- `Parser/JsonConfigParser.cs`
- `Parser/CsvConfigParser.cs`
