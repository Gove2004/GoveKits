# GoveKits Storage 模块

GoveKits Runtime Storage 是一套面向工程化项目的资源与数据基础设施，覆盖 配置表加载、资源热更新、读写存档、热更程序集、音频系统、多语言系统 等关键能力。

它与 Core、Unit 模块协同工作，强调高可扩展、低侵入、可替换实现，适用于中大型 Unity 项目。

## 目录结构

```text
Storage/
├── Config/                            # 配置系统（反射扫描 + 多解析器）
│   ├── ConfigCore.cs                  # 配置加载入口
│   ├── ConfigBindingScanner.cs        # [ConfigPath] 自动扫描
│   ├── IConfigData.cs                 # 配置行标记接口
│   └── Parser/
│       ├── IConfigParser.cs           # 配置解析器接口
│       ├── JsonConfigParser.cs        # JSON 解析器
│       └── CsvConfigParser.cs         # CSV 解析器
├── Res/                               # YooAsset 资源系统封装
│   ├── ResCore.cs                     # 资源加载 / 更新主入口
│   ├── PackageConfig.cs               # 包配置（离线/联机）
│   └── UpdateCallbacks.cs             # 热更新回调集合
├── Save/                              # 存档与偏好设置
│   ├── SaveCore.cs                    # 同步/异步存档
│   ├── PrefsCore.cs                   # PlayerPrefs 简封装
│   ├── AutoSaveBehaviour.cs           # 自动存档组件
│   └── Serializer/
│       ├── ISerializer.cs             # 序列化器接口
│       ├── JsonSerializer.cs          # JSON 序列化
│       └── MsgPackSerializer.cs       # MessagePack 序列化
├── Hotfix/
│   └── HotfixCore.cs                  # HybridCLR 热更加载与入口调用
└── Extension/
    ├── Audio/                         # 音频系统扩展
    │   ├── AudioCore.cs
    │   ├── AudioSO.cs
    │   └── AudioChannel.cs
    └── Localization/                  # 多语言系统扩展
        ├── LocalizationCore.cs
        ├── LocalizationConfigData.cs
        ├── LocalizationComponent.cs
        ├── LocalizationConfig.cs
        └── LanguageCode.cs
```

## 核心设计理念

1. 数据驱动优先
通过 ConfigCore + ResCore，将配置和资源加载统一为可注入、可替换的管线。

2. 运行时解耦
存档、资源、热更、扩展能力均提供静态入口，业务层仅依赖接口和约定。

3. 工程化可扩展
Parser、Serializer、PackageConfig 均可扩展；扩展模块（Audio/Localization）可独立启停。

## 模块使用指南

### 1. Config 配置系统

通过 ConfigPathAttribute 标记配置类型，ConfigCore 在初始化时自动扫描并加载。

```csharp
using GoveKits.Runtime.Storage;

[ConfigPath("Assets/Resources/Config/Localization.csv")]
public class LocalizationConfigData : IConfigData
{
    public string Key;
    public string ChineseCN;
    public string EnglishUS;
}

// 启动时注入解析器并初始化
ConfigCore.InfuseParser(new CsvConfigParser());
ConfigCore.InfuseParser(new JsonConfigParser());
ConfigCore.Initialize();

// 查询配置
var allRows = ConfigCore.LoadAll<LocalizationConfigData>();
var row = ConfigCore.LoadOne<LocalizationConfigData>(x => x.Key == "UI.Start");
```

### 2. Res 资源与热更新系统

基于 YooAsset 的封装，支持离线/联机包初始化、热更新下载、同步/异步资源加载。

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage;

async UniTask InitResAsync()
{
    var callbacks = new UpdateCallbacks
    {
        OnCheckVersionBegin = () => { },
        OnCheckVersionSuccess = version => { },
        OnDownloadUpdate = data => { }
    };

    // 编辑器下自动模拟；打包后自动离线模式
    var cfg = new AutoOfflinePackageConfig("DefaultPackage");
    bool ok = await ResCore.PackageWorkflowAsync(cfg, callbacks);
    if (!ok) return;

    // 资源加载（默认包可省略包名前缀）
    var handle = ResCore.LoadAssetAsync<UnityEngine.GameObject>("UI/Panel/MainPanel.prefab");
    await handle.Task;
    if (handle.Status == YooAsset.EOperationStatus.Succeed)
    {
        var go = handle.InstantiateSync();
    }
    ResCore.Release(handle);
}
```

### 3. Save 存档系统

SaveCore 支持同步/异步、原子写入、自动扩展名补全；序列化策略可自由替换。

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage;

[System.Serializable]
public class PlayerSaveData
{
    public int Level;
    public float Hp;
}

async UniTask SaveExample()
{
    // 1) 初始化序列化器（推荐仅初始化一次）
    SaveCore.Initialize(new JsonSerializer(), rootFolder: "Saves");

    // 2) 保存
    var data = new PlayerSaveData { Level = 10, Hp = 125.5f };
    await SaveCore.SaveAsync("slot1/player", data);

    // 3) 读取
    var loaded = await SaveCore.LoadOrDefaultAsync("slot1/player", new PlayerSaveData());
}
```

PrefsCore 用于轻量偏好存储：

```csharp
PrefsCore.SetFloat("Audio.Master", 0.8f);
PrefsCore.Save();
float v = PrefsCore.GetFloat("Audio.Master", 1f);
```

AutoSaveBehaviour 可注册自动保存对象：

```csharp
AutoSaveBehaviour.Instance.Register("player", "slot1/player", () => currentPlayerData);
// 在场景切换前也可手动触发
AutoSaveBehaviour.Instance.SaveAll();
```

### 4. Hotfix 热更系统（HybridCLR）

提供 AOT 元数据补充、热更程序集加载、入口方法反射调用。

```csharp
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Storage;

async UniTask StartHotfixAsync()
{
    // 1) 补充 AOT 元数据
    bool aotOk = await HotfixCore.LoadAotMetadataAsync(new[]
    {
        "mscorlib.dll.bytes",
        "System.dll.bytes"
    });
    if (!aotOk) return;

    // 2) 加载热更程序集
    var ass = await HotfixCore.LoadHotfixAssemblyAsync("Hotfix.dll.bytes");
    if (ass == null) return;

    // 3) 调用入口
    HotfixCore.StartEntryMethod("Hotfix", "Hotfix.Entry", "Main");
}
```

### 5. Extension.Audio 音频扩展

支持多通道音量、BGM 淡入淡出、动态音轨池、AudioSO 驱动播放。

```csharp
using GoveKits.Runtime.Storage;

AudioCore.Initialize();
AudioCore.SetVolume(AudioChannel.Master, 1f);
AudioCore.SetVolume(AudioChannel.BGM, 0.6f);
AudioCore.SetVolume(AudioChannel.SFX, 0.8f);

// 播放 ScriptableObject 配置音频
AudioCore.Play(clickAudioSO);

// 切换 BGM（自动淡变）
AudioCore.PlayBGM(bgmClip, fadeTime: 1f, pitch: 1f);
```

### 6. Extension.Localization 多语言扩展

支持语言切换、缓存命中读取、UGUI/TMP 组件自动刷新。

```csharp
using GoveKits.Runtime.Storage;

// 先保证 ConfigCore 已初始化并已加载 LocalizationConfigData
LocalizationCore.Initialize();

string title = LocalizationCore.GetText("UI.Title");
LocalizationCore.SwitchLanguage(LanguageCode.EnglishUS);
```

挂载 LocalizationComponent 到 Text/TMP_Text，即可根据 Key 自动更新文本与字体。

## 推荐初始化顺序

```csharp
// 1. 资源系统
await ResCore.PackageWorkflowAsync(new AutoOfflinePackageConfig("DefaultPackage"), null);

// 2. 配置系统
ConfigCore.InfuseParser(new CsvConfigParser());
ConfigCore.InfuseParser(new JsonConfigParser());
ConfigCore.Initialize();

// 3. 存档系统
SaveCore.Initialize(new MsgPackSerializer(), "Saves");

// 4. 业务扩展
AudioCore.Initialize();
LocalizationCore.Initialize();
```

## 最佳实践与注意事项

1. SaveCore 必须先 Initialize 再读写，且建议全局只初始化一次。
2. ConfigCore 初始化前必须先 InfuseParser；否则会出现格式不支持异常。
3. ResCore 资源句柄用完后记得 Release，避免引用导致资源无法卸载。
4. HotfixCore 在编辑器模式下会跳过 AOT 补充，这是正常行为。
5. AudioCore 与 LocalizationCore 建议在游戏主入口统一初始化，退出流程调用对应清理逻辑（如 Clear/OnShutdown）。
