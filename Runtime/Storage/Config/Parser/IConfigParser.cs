using System.Collections.Generic;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置解析器接口。
    /// </summary>
    public interface IConfigParser
    {
        /// <summary>
        /// 支持的扩展名（小写，带点）。
        /// 例如: json / csv
        /// </summary>
        IReadOnlyList<string> Extensions { get; }

        /// <summary>
        /// 将原始内容解析为配置对象列表。
        /// </summary>
        /// <remarks>
        /// 若 text 为空，解析器应自行从 bytes 构建文本或二进制视图。
        /// </remarks>
        List<T> Parse<T>(byte[] bytes, string text)
            where T : class, IConfigData, new();
    }
}
