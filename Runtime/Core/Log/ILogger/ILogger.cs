namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 日志器接口
    /// 定义所有日志实现类必须遵循的规范
    /// 支持多种日志输出方式的扩展（如文件、控制台、网络等）
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录日志方法
        /// </summary>
        /// <param name="tag">日志标签，用于标识日志来源模块</param>
        /// <param name="message">日志消息内容</param>
        /// <param name="level">日志等级，用于过滤和分类</param>
        /// <param name="colorHex">日志显示颜色（十六进制格式），可选参数</param>
        void Log(string tag, string message, LogLevel level, string colorHex = null);
    }
}