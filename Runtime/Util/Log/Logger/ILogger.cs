namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 日志输出后端接口。
    /// LogCore 是统一调用点，ILogger 才是真正的扩展点。
    /// 实现此接口即可接入自定义输出目标（如 Unity 控制台、文件、远程服务器等）。
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 输出一条格式化日志。
        /// colorHex 参数由 LogCore 保证非 null，实现层可直接使用。
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="tag">日志标签，用于标识来源模块</param>
        /// <param name="message">日志消息内容</param>
        /// <param name="colorHex">富文本颜色代码（十六进制）</param>
        void Log(LogLevel level, string tag, string message, string colorHex = null);
        /// <summary>
        /// 关闭日志后端，释放文件句柄、网络连接等外部资源。
        /// </summary>
        void Close();
    }
}