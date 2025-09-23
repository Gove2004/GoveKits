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

        // 可根据需要添加事件通用属性
    }
}