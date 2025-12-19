# Storage 模块

数据持久化与资源管理系统：提供游戏存档、配置表、资源加载的完整解决方案。

## 模块结构

### 三大子系统

| 子系统 | 职能 | 关键类 |
|--------|------|--------|
| **Save** | 游戏存档持久化 | SaveManager, ISaveable, AutoSave |
| **Config** | 配置表加载与查询 | ConfigManager, IConfigData |
| **Res** | 多模式资源加载 | ResManager, IResLoader, ResourceLoader, AddressableLoader, AssetBundleLoader |

---

## Save（存档系统）

**职能**：提供跨平台的游戏存档存储与加载。

### 核心特性

- **Protobuf 序列化**：高效、紧凑的二进制格式
- **原子操作**：使用临时文件 + 原子替换防止损坏
- **自动目录管理**：自动创建 `persistentDataPath/Saves/`
- **编辑器友好**：支持条件编译日志记录

### 关键类

| 类 | 职能 |
|----|------|
| **SaveManager** | 静态存档管理器，提供 SaveData/LoadData API |
| **ISaveable** | 接口，定义对象的存档/加载行为 |
| **AutoSave** | 自动存档组件（定时保存或触发条件保存） |

---

## Config（配置系统）

**职能**：加载和管理游戏配置表（Hero 配置、副本配置等）。

### 核心特性

- **反射自动扫描**：发现所有 IConfigData 实现类
- **约定优先**：类名自动推导 JSON 文件名
- **多模式加载**：通过 ResManager 支持 Resources/Addressables/AssetBundle
- **泛型查询**：支持按 ID 快速查询配置对象

---

## Res（资源管理系统）

**职能**：统一管理三种资源加载模式。

### 加载模式对比

| 模式 | 优点 | 缺点 | 适用场景 |
|------|------|------|---------|
| **Resources** | 简单易用，无配置 | 包体大，无法灵活更新 | 开发测试、小型项目 |
| **Addressables** | 灵活标签、支持更新 | 配置复杂 | 中大型项目、热更新 |
| **AssetBundle** | 精细控制、极致优化 | 手动依赖管理复杂 | 超大型项目 |

### 关键类

| 类 | 职能 |
|----|------|
| **ResManager** | 静态管理器，提供统一加载 API |
| **IResLoader** | 加载器接口 |
| **ResourceLoader** | Resources 加载实现 |
| **AddressableLoader** | Addressables 加载实现 |
| **AssetBundleLoader** | AssetBundle 加载实现 |

---

## 使用示例

### 初始化与加载

```csharp
// 1. 初始化资源系统（优先 Addressables）
ResManager.Initialize(ResType.Addressable);

// 2. 初始化配置系统
ConfigManager.Initialize("Config/Json");

// 3. 同步加载资源
var prefab = ResManager.Load<GameObject>("Prefabs/Player");

// 4. 异步加载资源
var mesh = await ResManager.LoadAsync<Mesh>("Models/Enemy");

// 5. 查询配置
var heroConfig = ConfigManager.GetConfig<HeroConfig>();
```

### 存档保存

```csharp
// 1. 实现 ISaveable
public class Player : ISaveable
{
    public void Save()
    {
        var data = new PlayerData { /* 填充数据 */ };
        SaveManager.SaveData(data, "player/data.pb");
    }

    public void Load()
    {
        var data = new PlayerData();
        SaveManager.LoadData(data, "player/data.pb");
    }
}

// 2. 启动自动保存
var autoSave = gameObject.AddComponent<AutoSave>();
autoSave.SetTarget(player);
autoSave.AutoSaveInterval = 60f;
```

---

## 最佳实践

1. **使用 Protobuf**：高效、紧凑的二进制格式
2. **版本管理**：配置表添加版本号字段便于迁移
3. **异步加载**：关键资源使用异步避免卡顿
4. **缓存管理**：不频繁使用的资源主动 Unload
5. **定时保存**：关键操作后保存，定期自动保存

---

## 相关文档

- [Events 事件系统](../../Utility/Events/README.md)
- [Editor/CodeGenerator Protobuf 代码生成](../../Editor/CodeGenerator/README.md)