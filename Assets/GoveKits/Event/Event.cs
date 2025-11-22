


using System;

namespace GoveKits.Event
{
    /// <summary>
    /// 基础事件类，携带公共属性数据
    /// </summary>
    public class DataEvent
    {
        /// <summary>
        /// 事件唯一标识符
        /// </summary>
        public Guid EventId { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// 事件创建时间
        /// </summary>
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// 事件来源（可选，用于调试）
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 自定义数据（扩展用）
        /// </summary>
        public object UserData { get; set; }
    }


    /// <summary>
    /// 事件管道类型
    /// </summary>
    public enum EventChannel
    {
        /// <summary> 默认管道，全局事件 </summary>
        Global,
        /// <summary> UI相关事件 </summary>
        UI,
        /// <summary> 游戏逻辑事件 </summary>
        Gameplay,
        /// <summary> 网络事件 </summary>
        Network,
        /// <summary> 音频事件 </summary>
        Audio,
        /// <summary> 调试事件 </summary>
        Debug,
        /// <summary> 自定义管道1 </summary>
        Custom1,
        /// <summary> 自定义管道2 </summary>
        Custom2
    }
}