namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 音频通道枚举。
    /// </summary>
    public enum AudioChannel
    {
        /// <summary>全局主音量（仅用于音量控制，不可播放）</summary>
        Master = 0,
        /// <summary>背景音乐（单通道，支持渐变）</summary>
        BGM = 1,
        /// <summary>常规音效（多通道）</summary>
        SFX = 2,
        /// <summary>UI音效（多通道）</summary>
        UI = 3,
        /// <summary>角色语音（多通道）</summary>
        Voice = 4,
        /// <summary>环境音（多通道）</summary>
        Ambient = 5,
    }
}
