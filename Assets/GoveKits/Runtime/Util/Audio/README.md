# Audio 模块文档

## 文档速览
- 目标: 提供统一的 BGM/SFX/UI/Voice 播放入口，并持久化音量配置。
- 核心入口: `AudioCore.Init`、`AudioCore.PlayBGM`、`AudioCore.PlaySFX`、`AudioCore.PlayUI`、`AudioCore.PlayVoice`、`AudioCore.SetVolume`。
- 依赖模块: `ResCore`(资源加载/释放) 与 `PrefsCore`(音量持久化)。

## 阅读路径
1. 先看 `AudioCore.cs` 了解初始化、播放和资源释放流程。
2. 再看 `AudioChannel.cs` 了解音量通道定义。
3. 最后结合 `ResCore` 理解音频资源路径与引用释放策略。

## 设计理念
- 单例化静态入口: 通过 `AudioCore` 统一管理全局音频状态。
- 通道分离: 将 Master/BGM/SFX/UI/Voice 拆分，便于独立调节。
- 自动持久化: 音量设置写入 `PrefsCore`，重启后自动恢复。
- 引用释放: 短音效播放后按时释放资源，减少资源常驻。

## 架构介绍
- `AudioCore`: 音频系统门面，负责初始化、音量管理、播放控制。
- `AudioChannel`: 音量通道枚举。
- `ResCore.Load/Release`: 负责加载和释放 `AudioClip`。
- `PrefsCore.GetFloat/SetFloat/Save`: 负责保存用户音量设置。

## 快速开始
### 1) 启动时初始化 AudioCore
```csharp
using GoveKits.Runtime.Util;

public class GameBootstrap : UnityEngine.MonoBehaviour
{
    private void Awake()
    {
        AudioCore.Init();
    }
}
```

### 2) 播放 BGM 与音效
```csharp
using GoveKits.Runtime.Util;

public class AudioDemo : UnityEngine.MonoBehaviour
{
    private void Start()
    {
        // 对应 Resources/Audio/BGM/MainTheme
        AudioCore.PlayBGM("Audio/BGM/MainTheme", 0.8f);
    }

    public void OnClickButton()
    {
        // 对应 Resources/Audio/UI/Click
        AudioCore.PlayUI("Audio/UI/Click");
    }
}
```

### 3) 调整并保存音量
```csharp
using GoveKits.Runtime.Util;

public static class AudioSettingLogic
{
    public static void ApplyBgmSlider(float value)
    {
        AudioCore.SetVolume(AudioChannel.BGM, value);
    }

    public static void ApplyMasterSlider(float value)
    {
        AudioCore.SetVolume(AudioChannel.Master, value);
    }
}
```

## 注意事项
- 请先调用 `AudioCore.Init`，再执行播放与调音接口。
- 当前播放接口默认使用 `ResLoadType.Resources`，路径应与 `Resources` 目录匹配且不带扩展名。
- `PlayBGM` 会在切歌时释放旧 BGM 资源；`PlaySFX/PlayUI/PlayVoice` 在剪辑时长结束后释放。
- `SetVolume` 会立即写入 `PrefsCore` 并 `Save`，适合设置面板实时生效。
- 如果你需要 3D 空间音效，建议新增独立播放接口而不是复用当前 2D 池。

## 相关跳转
- `AudioCore.cs`
- `AudioChannel.cs`
- `../Storage/Res/ResCore.cs`
- `../Storage/Save/PrefsCore.cs`
