# Storage 模块

GoveKits 存储子系统。提供资源管理、配置表加载、存档读写、热更新、音频引擎和多语言本地化等存储与集成能力。所有 Storage Core 由 `GoveCore` 统一管理，按依赖顺序初始化。

## 架构概览

```
GoveCore
├── PrefsCore        → PlayerPrefs 封装，偏好设置与音量持久化
├── ResCore          → YooAsset 包裹管理、资源加载/卸载、热更新工作流
├── ConfigCore       → 配置表扫描 + 解析（CSV/JSON），谓词查询
├── SaveCore         → 存档读写（原子写入），同步/异步 API
├── HotfixCore       → HybridCLR 热更加载（AOT 元数据 + DLL 加载 + 入口调用）
├── AudioCore        → 多通道音频引擎（BGM 淡入淡出、动态音源池）
└── LocalizationCore → 多语言文本系统，配置表驱动，字体切换

依赖链：
PrefsCore → ResCore → ConfigCore → HotfixCore
                ↘                    ↗
            SaveCore (独立分支)
                ↘
            AudioCore
ConfigCore → LocalizationCore
```

## 初始化顺序

```csharp
// 1. 资源包裹初始化
await ResCore.InitPackageAsync(new AutoOfflinePackageConfig("GamePackage"));

// 2. 配置表加载（必须在 LocalizationCore 之前）
ConfigCore.Setup();

// 3. 音频引擎初始化
AudioCore.Setup(initialPoolSize: 16);

// 4. 多语言初始化
LocalizationCore.Setup();

// 5. 关闭
GoveCore.Close();
```

## PrefsCore — 本地偏好设置

封装 PlayerPrefs 操作，支持基本类型和音频通道音量持久化。

### 用法

```csharp
// 基本类型
PrefsCore.SetInt("level", 42);
int level = PrefsCore.GetInt("level", 1);  // 第二个参数是默认值

PrefsCore.SetFloat("musicVol", 0.8f);
float vol = PrefsCore.GetFloat("musicVol");

PrefsCore.SetBool("fullscreen", true);
bool fullscreen = PrefsCore.GetBool("fullscreen");

PrefsCore.SetString("playerName", "Hero");
string name = PrefsCore.GetString("playerName");

// 检查键是否存在
bool hasLevel = PrefsCore.HasKey("level");

// 删除
PrefsCore.DeleteKey("level");
PrefsCore.DeleteAll();  // 清空所有键值

// 持久化到磁盘
PrefsCore.Save();
```

### 音频音量持久化

```csharp
// 设置音频音量（自动保存到 PlayerPrefs）
PrefsCore.SetAudioVolume(AudioChannel.BGM, 0.8f);
float vol = PrefsCore.GetAudioVolume(AudioChannel.BGM);
```

## ResCore — 资源管理系统

基于 YooAsset 的资源管理封装，支持离线模式和 CDN 热更新模式。

### 包裹初始化

```csharp
// 离线模式（编辑器模拟 / 生产离线）
var offlineConfig = new AutoOfflinePackageConfig("GamePackage");
await ResCore.InitPackageAsync(offlineConfig);

// 主机模式（CDN 热更新）
var hostConfig = new AutoHostPackageConfig(
    name: "HotfixPackage",
    cdn: "https://cdn.example.com",
    fallback: "https://fallback.example.com"
);
await ResCore.InitPackageAsync(hostConfig, setAsDefault: true);
```

### 资源加载

```csharp
// 异步加载资源
var handle = ResCore.LoadAssetAsync<GameObject>("Prefab/Player");
await handle.Task;
var prefab = handle.AssetObject as GameObject;

// 指定包裹加载
var handle2 = ResCore.LoadAssetAsync<AudioClip>("Hotfix:sound_battle.mp3");

// 同步加载（仅编辑器模拟模式可用）
var syncHandle = ResCore.LoadAssetSync<TextAsset>("Config/Enemy");

// 实例化（自动挂载到父节点）
var go = await ResCore.InstantiateAsync("Prefab/NPC", parent: transform);

// 场景加载
var sceneHandle = ResCore.LoadSceneAsync("Scene/Battle", LoadSceneMode.Single);

// 释放资源加载句柄
ResCore.Release(handle);
```

### 热更新工作流

```csharp
var callbacks = new UpdateCallbacks
{
    OnCheckVersionBegin = () => Debug.Log("开始检查版本"),
    OnCheckVersionSuccess = (version) => Debug.Log($"最新版本: {version}"),
    OnCheckVersionFailed = (error) => Debug.LogError($"版本检查失败: {error}"),
    OnDownloadBegin = (count, total) => Debug.Log($"开始下载，共 {count} 个文件"),
    OnDownloadUpdate = (data) => Debug.Log($"下载进度: {data.Progress:P1}"),
    OnDownloadFinish = (data) => Debug.Log($"下载完成: {data.PackageName}")
};

bool success = await ResCore.PackageWorkflowAsync(packageConfig, callbacks);
```

### 内存管理

```csharp
// 卸载未使用的资源
ResCore.UnloadUnusedAssets();

// 销毁指定包裹
ResCore.DestroyPackage("OldPackage");

// 异步卸载包裹
await ResCore.UnloadPackage("Package");
```

## ConfigCore — 配置表系统

扫描并加载标记了 `[ConfigPath]` 的配置类，支持 CSV 和 JSON 两种格式，提供谓词查询 API。

### 定义配置类型

```csharp
// CSV 列名: "ID", "名称", "攻击力", "暴击率,默认=0.05"
[ConfigPath("Config/Weapon", "csv")]
public class WeaponConfig : IConfigData
{
    // 方式 1: 字段名与列名一致（忽略大小写）
    public int ID;

    // 方式 2: 使用 [ConfigField] 指定列别名
    [ConfigField("名称")]
    public string Name;

    // 方式 3: 列别名 + 默认值（配置为空时回退）
    [ConfigField("暴击率,默认=0.05")]
    public float CritRate;

    // 属性同样支持
    [ConfigField("描述")]
    public string Desc { get; set; }
}
```

### 查询配置

```csharp
// Load / Get — 带谓词过滤（首次加载，之后从缓存查询）
var weapons = ConfigCore.Load<WeaponConfig>(w => w.Attack > 100);
var weapons2 = ConfigCore.Get<WeaponConfig>(w => w.ID == 1001);

// LoadAll / GetAll — 全量加载
var allWeapons = ConfigCore.LoadAll<WeaponConfig>();
var allWeapons2 = ConfigCore.GetAll<WeaponConfig>();

// LoadOne / GetOne — 首条匹配
var weapon = ConfigCore.LoadOne<WeaponConfig>(w => w.ID == 1001);
var weapon2 = ConfigCore.GetOne<WeaponConfig>(w => w.Name == "屠龙刀");
```

### JSON 配置

```csharp
[ConfigPath("Config/Item", "json")]
public class ItemConfig : IConfigData { ... }

// JsonConfigParser 支持: List<T> / Dictionary<int,T> / Dictionary<string,T> / 单对象
```

## SaveCore — 存档系统

支持同步/异步 API，原子写入（先写临时文件再重命名）防止存档损坏。

### 基本用法

```csharp
// 同步保存/加载
var playerData = new PlayerData { Name = "Hero", Level = 10 };
SaveCore.Save("player", playerData);

var loaded = SaveCore.Load<PlayerData>("player");
var fallback = SaveCore.LoadOrDefault<PlayerData>("missing", defaultPlayerData);

// 异步保存/加载
await SaveCore.SaveAsync("save/slot1", slotData);
var asyncLoaded = await SaveCore.LoadAsync<PlayerData>("save/slot1");

// 文件操作
bool exists = SaveCore.Exists("player");
SaveCore.Delete("player");
string[] allFiles = SaveCore.GetAllFiles("*");
```

### AutoSaveBehaviour — 自动保存组件

挂载到场景中实现定时自动存档：

```csharp
var autoSave = gameObject.AddComponent<AutoSaveBehaviour>();
autoSave.intervalSeconds = 30f;  // 每 30 秒自动保存

// 注册存档项（键名, 文件名, 数据获取回调）
autoSave.Register<PlayerData>("player", "player", () => currentPlayerData);
autoSave.Register<StageData>("stage", "stage", () => currentStageData);

// 手动触发保存
autoSave.SaveAll();

// 取消注册
autoSave.Unregister("player");
```

## HotfixCore — HybridCLR 热更新

封装 HybridCLR 热更加载流程：AOT 泛型元数据加载 → 热更程序集加载 → 入口方法调用。

### 标准热更新流程

```csharp
// 1. 加载 AOT 泛型元数据（真机 IL2CPP 必需）
await HotfixCore.LoadAotMetadataAsync(
    new[] { "HybridCLRData.dll", "mscorlib.dll" },
    packageName: "Hotfix"
);

// 2. 加载热更程序集
Assembly hotfixAssembly = await HotfixCore.LoadHotfixAssemblyAsync("Hotfix:Hotfix.dll.bytes");

// 3. 启动入口方法
bool success = HotfixCore.StartEntryMethod(
    assemblyName: "Hotfix",
    className: "GameEntry",
    methodName: "Start",
    args: null
);
```

## AudioCore — 音频引擎

多通道音频引擎，支持 BGM 淡入淡出、动态音源池、通道音量持久化。

### 初始化

```csharp
AudioCore.Setup(initialPoolSize: 16);
```

### 播放音频

```csharp
// 使用 AudioSO 资源项
AudioSO bgmMusic = Resources.Load<AudioSO>("BGM/Battle");
AudioCore.Play(bgmMusic);

// BGM 淡入淡出（自动交叉渐变）
AudioCore.PlayBGM(clip: battleMusic, fadeTime: 2f, pitch: 1f);

// 动态播放（SFX/UI/Voice/Ambient 通道）
AudioCore.PlayDynamic(
    channel: AudioChannel.SFX,
    clip: shootSound,
    volScale: 0.8f,
    pitch: 1.2f,
    loop: false,
    position: transform.position  // 传入则为 3D 空间音效
);
```

### 音量控制

```csharp
// 获取/设置音量（自动保存到 PlayerPrefs）
float bgmVol = AudioCore.GetVolume(AudioChannel.BGM);
AudioCore.SetVolume(AudioChannel.BGM, 0.5f);

// 暂停/恢复所有音频
AudioCore.PauseAll();
AudioCore.ResumeAll();

// 停止指定通道或 BGM
AudioCore.StopAllChannel(AudioChannel.SFX);
AudioCore.StopBGM();
```

## LocalizationCore — 多语言本地化

从配置表加载翻译数据，支持运行时语言切换和字体映射。

### 初始化

```csharp
// 确保 ConfigCore 已初始化并加载了本地化配置
LocalizationCore.Setup();
```

### 定义本地化配置

```csharp
// 配置表列名: "Key", "ChineseCN", "EnglishUS", "JapaneseJP"
[ConfigPath("Config/Localization", "csv")]
public class MyLocalizationConfigData : ILocalizationConfigData
{
    // Key 字段由基类提供
    // ChineseCN / EnglishUS / JapaneseJP 字段由解析器通过反射读取
}
```

### 使用

```csharp
// 获取本地化文本
string text = LocalizationCore.GetText("welcome_message");

// 字符串扩展方法
string text2 = "welcome_message".I18n();

// 切换语言
LocalizationCore.SwitchLanguage(LanguageCode.EnglishUS);

// 监听语言切换
LocalizationCore.OnLanguageChanged += () => Debug.Log("语言已切换");

// 获取当前语言
LanguageCode current = LocalizationCore.CurrentLanguage;

// 获取当前语言的字体资源（需 TMP）
TMP_FontAsset font = LocalizationCore.GetCurrentFont();
```

### LocalizationComponent — 自动本地化组件

挂载在 Text / TextMeshProUGUI 对象上，语言切换时自动更新文本：

```csharp
var comp = gameObject.AddComponent<LocalizationComponent>();
comp.Key = "welcome_message";

// 动态修改键
comp.SetKey("new_key");
```

## 注意事项

- **初始化顺序**：ResCore → ConfigCore → LocalizationCore 必须按此顺序调用 Setup
- **配置表类型**：必须实现 `IConfigData` 并标注 `[ConfigPath]`
- **[ConfigField]**：支持 `"列名"` 和 `"列名,默认=值"` 两种格式
- **ISerializer**：SaveCore 默认注入 `JsonSerializer`，可通过构造函数替换为 `MsgPackSerializer`
- **AudioCore**：创建 `DontDestroyOnLoad` 根物体，Close 时会销毁
- **LocalizationComponent**：依赖 `TMP_PRESENT` 宏，无 TMP 时自动降级到 `UnityEngine.UI.Text`
- **HotfixCore**：编辑器模式下跳过 AOT 元数据加载，直接从 AppDomain 查找程序集
- **ICore 生命周期**：所有 Storage Cores 通过 `GoveCore.Close()` 统一关闭
