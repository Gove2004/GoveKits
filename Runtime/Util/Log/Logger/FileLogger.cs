using System;
using System.IO;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 文件日志后端，将日志以纯文本格式写入指定文件。
    /// 每条日志一行，包含时间戳、日志等级和标签。
    /// 内部使用缓冲写入：积累一定条数后统一刷盘，Warning 及以上立即刷盘，Close 时刷盘并释放句柄。
    /// </summary>
    public class FileLogger : ILogger
    {
        // 缓冲达到该条数时统一刷盘
        private const int FlushThreshold = 32;

        private readonly string filePath;
        private StreamWriter writer;
        private int bufferedCount;

        /// <summary>
        /// 创建 FileLogger 实例，日志文件所在目录在首次写入时自动创建。
        /// </summary>
        /// <param name="filePath">日志文件的完整路径</param>
        public FileLogger(string filePath)
        {
            this.filePath = filePath;

            // 退出兜底钩子：未走 GoveCore.Close（崩溃/直接退出）时避免丢失缓冲日志
            UnityEngine.Application.quitting += FlushOnQuit;
            AppDomain.CurrentDomain.ProcessExit += FlushOnProcessExit;
        }

        private void FlushOnQuit() => FlushBuffer();

        private void FlushOnProcessExit(object sender, EventArgs e) => FlushBuffer();

        private void FlushBuffer()
        {
            try
            {
                if (writer == null) return;
                writer.Flush();
                bufferedCount = 0;
            }
            catch
            {
                // 退出阶段刷盘失败：静默处理，避免异常处理产生新日志导致递归
            }
        }

        /// <summary>
        /// 将日志以纯文本写入文件，格式为 "[时间戳] [等级] [标签] 消息"。
        /// Warning 及以上等级立即刷盘，其余缓冲写入。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="tag">日志标签。</param>
        /// <param name="message">日志消息内容。</param>
        /// <param name="colorHex">颜色十六进制值（文件日志不使用，保留参数）。</param>
        public void Log(LogLevel level, string tag, string message, string colorHex = null)
        {
            try
            {
                EnsureWriter();

                writer.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {message}\n");
                bufferedCount++;

                // 错误与警告立即落盘，便于崩溃后排查；普通日志按阈值批量刷盘
                if (level >= LogLevel.Warning || bufferedCount >= FlushThreshold)
                {
                    writer.Flush();
                    bufferedCount = 0;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[FileLogger] 日志写入失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷盘、移除退出钩子并释放文件句柄（由 GoveCore.Close 调用）。
        /// </summary>
        public void Close()
        {
            FlushBuffer();
            UnityEngine.Application.quitting -= FlushOnQuit;
            AppDomain.CurrentDomain.ProcessExit -= FlushOnProcessExit;
            writer?.Dispose();
            writer = null;
            bufferedCount = 0;
        }

        private void EnsureWriter()
        {
            if (writer != null) return;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            writer = new StreamWriter(filePath, append: true);
        }
    }
}
