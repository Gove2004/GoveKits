# Localization 模块文档

## 文档速览
- 目标: 通过 `ConfigCore` 加载多语言文本，支持运行时切换语言与 TMP 字体跟随。
- 核心入口: `LocalizationCore.Initialize`、`LocalizationCore.GetText`、`LocalizationCore.SwitchLanguage`。
- UI 接入: `LocalizationComponent` 自动监听语言变更并刷新文案/字体。

## 阅读路径
1. 先看 `LocalizationCore.cs` 了解初始化、缓存与语言切换流程。
2. 再看 `LocalizationComponent.cs` 了解 UI 自动刷新逻辑。
3. 最后看 `LocalizationConfig.cs` 与 `LanguageCode.cs` 了解字体映射和语言枚举。

## 设计理念
- 配置驱动: 文案表通过 `[Config]` 注解声明，统一由 `ConfigCore` 加载。
- 缓存优先: 切换语言后生成当前语言字典，`GetText` 为 O(1) 查询。
- 兼容降级: 字段缺失时回退到 `EnglishUS`，避免直接显示空文本。
- 可选 TMP 依赖: 通过 `TMP_PRESENT` 条件编译，未安装 TMP 也可编译。

## 架构介绍
- `LocalizationTextRow`: 本地化行模型，对应配置表一行。
- `LocalizationCore`: 管理初始化、语言保存、文本缓存、字体查询。
- `LocalizationComponent`: 挂在 UI 文本对象上，自动刷新文本和字体。
- `LocalizationConfig`: 字体映射配置（语言 -> TMP_FontAsset）。
- `LanguageCode`: 语言枚举。

## 快速开始
### 1) 准备本地化配置表
`LocalizationTextRow` 已绑定以下配置声明:
```csharp
[Config("Config/Localization", ConfigFileType.Json, ConfigSourceType.Resources)]
public class LocalizationTextRow : IConfigData
{
    public string Key;
    public string ChineseCN;
    public string EnglishUS;
    public string Japanese;
    public string Korean;
}
```
将配置文件放到 `Resources/Config/Localization` 对应位置，并保证字段名与语言枚举一致。

### 2) 启动初始化并读取文本
```csharp
using GoveKits.Runtime.Util;

public class LocalizationBootstrap : UnityEngine.MonoBehaviour
{
    private void Awake()
    {
        LocalizationCore.Initialize();
    }

    private void Start()
    {
        string title = LocalizationCore.GetText("UI.Title");
        UnityEngine.Debug.Log(title);
    }
}
```

### 3) 切换语言
```csharp
using GoveKits.Runtime.Util;

public static class LanguageSettingLogic
{
    public static void SetEnglish()
    {
        LocalizationCore.SwitchLanguage(LanguageCode.EnglishUS);
    }

    public static void SetChinese()
    {
        LocalizationCore.SwitchLanguage(LanguageCode.ChineseCN);
    }
}
```

### 4) UI 自动刷新（推荐）
在 `TMP_Text` 或 `UnityEngine.UI.Text` 所在对象挂载 `LocalizationComponent`，并填写 `Key`。
切换语言时会自动触发 `OnLanguageChanged` 并刷新。

## LocalizationConfig 如何使用
- 在 Project 面板创建配置: `Create/GoveKits/Localization Config`。
- 将资源保存到 `Resources/Config/LocalizationConfig.asset`。
- 在 `FontSettings` 中为每个 `LanguageCode` 指定字体，`DefaultFont` 用于兜底。
- `LocalizationCore.Initialize` 时会自动执行:
  - `Resources.Load<LocalizationConfig>("Config/LocalizationConfig")`
- 启用 TMP 时，`LocalizationComponent` 会调用 `LocalizationCore.GetCurrentFont()` 自动换字库。

## 注意事项
- `LocalizationCore.Initialize` 内部会确保 `ConfigCore` 已初始化；建议仍在游戏启动流程尽早初始化，避免首帧阻塞。
- `GetText` 在未命中时返回 `#Key#`，方便快速发现漏配。
- 新增语言时需要同时更新:
  - `LanguageCode` 枚举
  - `LocalizationTextRow` 字段
  - 配置表列名
  - 字体映射 `LocalizationConfig`
- 若项目未安装 TMP，请不要定义 `TMP_PRESENT`，相关字体逻辑会自动剔除。

## 相关跳转
- `LocalizationCore.cs`
- `LocalizationComponent.cs`
- `LocalizationConfig.cs`
- `LanguageCode.cs`
- `../Storage/Config/ConfigCore.cs`
