using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置路径特性，用于标注配置类型对应的资源文件路径和文件格式扩展名。
    /// 将该特性应用到实现 IConfigData 的类上，ConfigBindingScanner 会自动发现并建立绑定。
    /// </summary>
    /// <example>
    /// [ConfigPath("Config/Weapon", "csv")]
    /// public class WeaponConfig : IConfigData { ... }
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigPathAttribute : Attribute
    {
        /// <summary>
        /// 资源文件路径（相对于资源包根目录）。
        /// </summary>
        public string FilePath { get; }
        /// <summary>
        /// 文件扩展名（不含点），用于匹配对应的 IConfigParser。
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// 创建配置路径特性。
        /// </summary>
        /// <param name="filePath">资源文件路径。</param>
        /// <param name="extension">文件扩展名。</param>
        public ConfigPathAttribute(string filePath, string extension)
        {
            FilePath = filePath;
            Extension = extension;
        }
    }

    /// <summary>
    /// 配置解析器接口。实现此接口以支持新的配置文件格式（如 JSON、CSV 等）。
    /// 解析器通过 Extensions 声明支持的格式，Parse 方法负责将原始字节或文本转换为配置对象列表。
    /// </summary>
    public interface IConfigParser
    {
        /// <summary>
        /// 支持的扩展名列表（小写，通常带前导点）。例如: ["json"]、["csv"]。
        /// </summary>
        IReadOnlyList<string> Extensions { get; }

        /// <summary>
        /// 将原始配置数据解析为目标类型的对象列表。
        /// </summary>
        /// <typeparam name="T">配置数据类型，必须实现 IConfigData 且有无参构造函数。</typeparam>
        /// <param name="bytes">配置文件的原始字节数据。</param>
        /// <param name="text">配置文件的文本内容；若为空则解析器应从 bytes 自行构建文本。</param>
        /// <returns>解析得到的配置对象列表。</returns>
        /// <remarks>若 text 为空，解析器应自行从 bytes 构建文本或二进制视图。</remarks>
        List<T> Parse<T>(byte[] bytes, string text) where T : class, IConfigData, new();
    }
}
