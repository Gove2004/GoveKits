namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 音频资源频道
    /// </summary>
    public enum AudioChannel
    {
        Master = 0,   // 全局主音量 (仅用于音量控制，不可播放)
        BGM = 1,      // 背景音乐 (单通道，支持渐变)
        SFX = 2,      // 常规音效 (多通道)
        UI = 3,       // UI音效 (多通道)
        Voice = 4,    // 角色语音 (多通道)
        Ambient = 5,  // 环境音 (示例：高扩展性体现，随意增加)
    }
}