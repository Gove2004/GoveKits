# Runtime Storage Save 开发手册

Save 模块提供统一的存档读写入口，支持 Json/Protobuf 序列化策略切换、同步与异步 API、以及自动定时保存能力。

## 文档速览

- 核心入口: `SaveCore`
- 数据协议: `ISaveData<T>`
- 自动保存: `AutoSaveBehaviour`
- 序列化扩展: `ISerializer` + `JsonSerializer`/`ProtobufSerializer`

## 阅读路径

1. 先看 `ISaveData<T>` 了解数据与物理存档的一一对应关系。
2. 再看 `SaveCore` 理解格式切换、同步/异步调用与原子写入。
3. 最后看 `AutoSaveBehaviour` 了解运行时自动保存接入方式。

## 设计理念

- 一个 `ISaveData<T>` 实例唯一对应一个物理存档文件（由 `RelativePath` 标识）。
- 业务数据与序列化策略解耦，序列化实现可替换。
- 既支持阻塞式同步接口，也支持 `UniTask` 异步接口。
- 使用临时文件 + 原子替换，降低写盘中断导致坏档的风险。

## 架构介绍

- `ISaveData<T>`: 业务存档对象协议。
- `SaveCore`:
  - 注册序列化器 (`RegisterSerializer`)
  - 切换格式 (`SetSerializerFormat`)
  - 同步读写 (`Save/Load`)
  - 异步读写 (`SaveAsync/LoadAsync`)
- `ISerializer`: 序列化器统一接口。
- `JsonSerializer`: 基于 `Newtonsoft.Json`。
- `ProtobufSerializer`: 基于 `Google.Protobuf`。
- `AutoSaveBehaviour`: 每隔固定时间批量触发异步保存。

## 快速开始

### 1. 定义存档对象

```csharp
using GoveKits.Runtime.Storage.Save;

public sealed class PlayerSaveData : ISaveData<PlayerState>
{
    private readonly PlayerRuntime _runtime;

    public PlayerSaveData(PlayerRuntime runtime)
    {
        _runtime = runtime;
    }

    public string RelativePath => "player/profile";

    public PlayerState Save()
    {
        return new PlayerState
        {
            Level = _runtime.Level,
            Gold = _runtime.Gold,
            Name = _runtime.Name
        };
    }

    public void Load(PlayerState state)
    {
        _runtime.Level = state.Level;
        _runtime.Gold = state.Gold;
        _runtime.Name = state.Name;
    }
}

public sealed class PlayerState
{
    public int Level;
    public int Gold;
    public string Name;
}
```

### 2. 使用 Json 同步存档

```csharp
using GoveKits.Runtime.Storage.Save;

SaveCore.SetSerializerFormat(SerializerType.Json);

var playerSave = new PlayerSaveData(playerRuntime);
SaveCore.Save(playerSave);
SaveCore.Load(playerSave);
```

### 3. 使用 Protobuf 异步存档

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage.Save;

public async UniTask SaveWithProto(ISaveData<PlayerPbData> save)
{
    SaveCore.SetSerializerFormat(SerializerType.Protobuf);
    await SaveCore.SaveAsync(save);
    await SaveCore.LoadAsync(save);
}
```

## 注意事项

- `RelativePath` 建议稳定，避免改名导致读取不到旧档。
- 当前统一使用 `.save` 作为文件后缀，切换格式时请确认迁移策略。
- 使用 Protobuf 时，数据类型必须实现 `IMessage` 并提供静态 `Parser`。
- 自动保存默认按 `RelativePath` 去重注册，同路径会覆盖旧注册项。

## 常见故障排查

- 现象: 保存后加载失败。
  - 排查: 检查当前 `SerializerType` 与写入时是否一致。
- 现象: Protobuf 抛出 `must implement IMessage`。
  - 排查: 检查 `ISaveData<T>` 中 `T` 是否为 protoc 生成类型。
- 现象: 自动保存未触发。
  - 排查: 检查 `AutoSaveBehaviour.autoSaveEnabled` 与 `saveIntervalSeconds`。
- 现象: 存档文件不存在。
  - 排查: 检查 `RelativePath` 是否为空或被动态改写。

## 相关跳转

- Root: [../../../../../README.md](../../../../../README.md)
- Runtime Core: [../../Core/README.md](../../Core/README.md)
- Runtime Unit: [../../Unit/README.md](../../Unit/README.md)
- 术语与命名规范: [../../../../../TERMINOLOGY.md](../../../../../TERMINOLOGY.md)
