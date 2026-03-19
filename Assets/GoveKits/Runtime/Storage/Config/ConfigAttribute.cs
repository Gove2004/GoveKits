using System;

namespace GoveKits.Runtime.Storage.Config
{
    /// <summary>
    /// 标记配置类型对应的文件路径、来源和解析器。
    /// </summary>
    /// <example>
    /// [Config("Configs/Enemy", ConfigFileType.Json, ConfigSourceType.Resources)]
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigAttribute : Attribute
    {
        public ConfigAttribute(string filePath, ConfigFileType parseType, ConfigSourceType sourceType = ConfigSourceType.Resources)
        {
            FilePath = filePath;
            ParseType = parseType;
            SourceType = sourceType;
        }

        /// <summary>
        /// 配置文件相对路径。
        /// Resources 来源可写带扩展名或不带扩展名。
        /// StreamingAssets 来源建议带扩展名。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 解析类型。
        /// </summary>
        public ConfigFileType ParseType { get; }

        /// <summary>
        /// 配置来源。
        /// </summary>
        public ConfigSourceType SourceType { get; }
    }
}