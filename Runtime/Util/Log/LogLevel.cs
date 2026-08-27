namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 日志等级枚举，数值越小代表信息越详细。
    /// 排序：Verbose &lt; Debug &lt; Info &lt; Warning &lt; Error。
    /// LogCore 仅输出不低于当前设定等级的日志。
    /// </summary>
    public enum LogLevel
    {
        /// <summary>最详细的日志等级，用于开发调试的琐碎信息。</summary>
        Verbose = 0,
        /// <summary>调试日志，用于跟踪程序运行流程。</summary>
        Debug   = 1,
        /// <summary>信息日志，用于记录一般性运行时消息。</summary>
        Info    = 2,
        /// <summary>警告日志，用于提示潜在问题但不影响运行。</summary>
        Warning = 3,
        /// <summary>错误日志，用于记录导致功能异常的问题。</summary>
        Error   = 4,
    }
}