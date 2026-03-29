namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 日志等级枚举
    /// 用于区分不同重要程度的日志消息
    /// 数值越小表示等级越低（越详细），数值越大表示等级越高（越严重）
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试等级 - 最详细的日志信息，用于开发调试
        /// </summary>
        Debug = 0,
        
        /// <summary>
        /// 信息等级 - 一般性信息记录
        /// </summary>
        Info = 1,
        
        /// <summary>
        /// 警告等级 - 需要注意但不影响程序运行的问题
        /// </summary>
        Warning = 2,
        
        /// <summary>
        /// 错误等级 - 严重影响程序运行的问题
        /// </summary>
        Error = 3
    }
}