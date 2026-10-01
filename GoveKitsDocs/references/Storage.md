# Runtime/Storage —— 存储与集成模块

资源加载（YooAsset）、配置表、存档、热更新（HybridCLR）、音频与多语言六大能力的统一入口。

## 模块架构

```
Runtime/Storage/
├── Res/                   资源加载 —— YooAsset 封装（包裹初始化/加载/释放）
├── Config/                配置表 —— 扫描 [ConfigPath] 标注 + 解析器加载 + 查询
├── Save/                  存档 —— 序列化保存/读取（默认 JSON，可换序列化器）
├── Hotfix/                热更新 —— HybridCLR AOT 元数据/热更程序集加载
└── Extension/
    ├── Audio/             音频 —— 多通道播放、BGM、音量控制
    └── Localization/      多语言 —— 语言切换、文本查询、TMP 字体映射
```

## Res —— 资源加载

基于 YooAsset，支持编辑器模拟/离线内置/CDN 热更三种模式。

```csharp
// 1. 初始化资源包（编辑器自动模拟模式）
var config = new AutoOfflinePackageConfig("Main");                          // 离线模式
// 或热更模式：
// var config = new AutoHostPackageConfig("Main", "https://cdn.example.com/game");

await ResCore.InitPackageAsync(config);                                     // 初始化（异步）

// 2. 加载资源
// 异步加载（句柄需要手动释放）
var handle = ResCore.LoadAssetAsync<Sprite>("Assets/UI/icon.png");
await handle.Task;
Sprite icon = handle.AssetObject as Sprite;
// ... 使用完毕
ResCore.Release(handle);                                                    // 释放句柄

// 便捷实例化（内部自动释放句柄）
var go = await ResCore.InstantiateAsync("Assets/Prefabs/Enemy.prefab", parent);

// 同步加载
var handle2 = ResCore.LoadAssetSync<TextAsset>("Assets/Configs/item.json");
ResCore.Release(handle2);

// 3. 卸载未引用资源 / 销毁包裹
ResCore.UnloadUnusedAssets();                        // 卸载未被引用的资源
ResCore.ClearCacheFiles("Main");                     // 清除下载缓存文件（非卸载包裹，仅释放磁盘缓存）
ResCore.DestroyPackage("Main");
ResCore.CloseAsync();                                // 完整关闭（先销毁包裹再释放，Close 后可重新 InitPackageAsync）
```

> 注意：`LoadAssetAsync` / `LoadAssetSync` 返回句柄，**用完必须 `ResCore.Release(handle)`**；`InstantiateAsync` 已内部自动释放。

## Config —— 配置表

扫描带 `[ConfigPath]` 标注的配置类型，自动加载解析进内存，提供类型安全查询。

```csharp
// 1. 定义配置数据（实现 IConfigData，标注资源路径与格式）
[ConfigPath("Assets/Configs/item.json", "json")]
public class ItemConfig : IConfigData
{
    public int Id;
    public string Name;
    public int Price;
}

// 2. 初始化（注册解析器 → Setup 扫描加载）
ConfigCore.AddParser(new JsonConfigParser());          // 支持 json / csv 等，可挂多个
ConfigCore.Setup();

// 3. 查询（Setup 后任意处调用）
var all = ConfigCore.LoadAll<ItemConfig>();                     // 全部配置
var swords = ConfigCore.Load<ItemConfig>(x => x.Name.Contains("剑"));  // 谓词过滤
var first = ConfigCore.LoadOne<ItemConfig>(x => x.Id == 1001);  // 取第一条匹配
```

## Save —— 存档

基于 `Application.persistentDataPath/Saves`，默认 JSON 序列化，支持自定义序列化器。

```csharp
// 1. 初始化（传 null 使用默认 JsonSerializer，或传自定义序列化器）
SaveCore.Setup(null);                                      // 默认 JsonSerializer

// 2. 保存 / 读取
var player = new PlayerSave { Gold = 999, Level = 5 };
SaveCore.Save("player.dat", player);                       // 保存（原子写入）
var loaded = SaveCore.Load<PlayerSave>("player.dat");      // 读取
var fallback = SaveCore.LoadOrDefault("setting.dat", new Settings());  // 不存在或文件损坏都返回默认

// 异步版本
await SaveCore.SaveAsync("player.dat", player);
var loaded2 = await SaveCore.LoadAsync<PlayerSave>("player.dat");

// 3. 管理
bool exists = SaveCore.Exists("player.dat");
SaveCore.Delete("player.dat");
string[] files = SaveCore.GetAllFiles("*.dat");
```

> 安全约束：所有路径参数相对存档根目录解析，`../` 上跳或绝对路径等离开存档根目录的路径会被拒绝并报错（防路径穿越）。

### AutoSave —— 自动存档（挂场景中的 AutoSaveBehaviour）

```csharp
// 注册：每 interval 秒自动把 getData 返回的数据保存到 path（key 用于注销）
autoSave.Register("player", "player.dat", () => new PlayerSave { Gold = 999 });
autoSave.SaveAll();                                  // 手动触发一次全量保存（如退到后台时）
autoSave.Unregister("player");                       // 注销
```

### Prefs —— 轻量键值（PlayerPrefs 封装）

```csharp
PrefsCore.SetInt("level", 5);
int level = PrefsCore.GetInt("level", 1);
PrefsCore.Save();                                    // 显式落盘
```

## Hotfix —— 热更新

HybridCLR 热更流程：加载 AOT 元数据 → 加载热更程序集 → 启动入口。

```csharp
// 1. 加载 AOT 补充元数据（从资源包）
await HotfixCore.LoadAotMetadataAsync(new[] { "mscorlib.dll", "System.dll" }, "Main");

// 2. 加载热更程序集（从资源包）
var assembly = await HotfixCore.LoadHotfixAssemblyAsync("Assets/Hotfix/Hotfix.dll");

// 3. 启动入口方法
HotfixCore.StartEntryMethod("Hotfix", "GameEntry", "Main", args);

// 4. 查询已加载程序集
var asm = HotfixCore.GetAssembly("Hotfix");
```

## Audio —— 音频

多通道播放（BGM/SFX/UI 等独立音量控制），支持 2D/3D、循环、音调随机。

```csharp
// 1. 初始化（预热音频源对象池）
AudioCore.Setup(initialPoolSize: 16);

// 2. 播放
// 用 AudioSO 资源（编辑器右键 Create → GoveKits/AudioSO 配置）
AudioCore.Play(hitAudioSO);                                // 播放一次（按 SO 的通道/音量/音调）

// 直接播放
AudioCore.PlayBGM(bgmClip, fadeTime: 1f);                  // 背景音乐（淡入）
AudioCore.PlayDynamic(AudioChannel.SFX, clip, volScale: 0.8f, position: transform.position);  // 3D 音效

// 3. 音量控制
AudioCore.SetVolume(AudioChannel.BGM, 0.5f);               // 各通道独立音量
float vol = AudioCore.GetVolume(AudioChannel.SFX);

// 4. 暂停 / 恢复 / 停止
AudioCore.PauseAll();
AudioCore.ResumeAll();
AudioCore.StopBGM();
AudioCore.StopAllChannel(AudioChannel.SFX);
```

## Localization —— 多语言

配置表驱动，支持语言切换、文本查询、TMP 字体映射。

```csharp
// 1. 初始化（可选：传回退语言，翻译缺失时降级显示，默认 EnglishUS）
LocalizationCore.Setup(LanguageCode.ChineseCN);

// 2. 获取文本 / 切换语言
string tip = LocalizationCore.GetText("ui.login.tip");     // 当前语言文本，缺 key 返回 "#key#"
LocalizationCore.SwitchLanguage(LanguageCode.EnglishUS);    // 切换语言（ChineseCN/EnglishUS/Japanese/Korean）

// 3. 监听语言变化（刷新 UI）
LocalizationCore.OnLanguageChanged += () => {
    tipText.text = LocalizationCore.GetText("ui.login.tip");
};
// 语言切换后会自动触发，配合 TMP 字体映射（LocalizationConfig 配置各语言字体）

// 4. 当前语言 / 字体
LanguageCode lang = LocalizationCore.CurrentLanguage;
var font = LocalizationCore.GetCurrentFont();              // TMP 字体
```
