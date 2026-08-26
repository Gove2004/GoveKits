using System;
using System.Reflection;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置字段特性，用于指定配置表中的列名别名和单元格为空时的默认值。
    /// 将该特性应用到配置类的字段或属性上，CSV 解析器会据此完成列名映射和默认值回填。
    /// </summary>
    /// <example>
    /// [ConfigField("名称")]           // 表头名为"名称"，映射到字段 Name
    /// [ConfigField("暴击率=0.05")] // 表头名为"暴击率"，空值时取 0.05
    /// public string Name;
    /// public float CritRate;
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ConfigFieldAttribute : Attribute
    {
        /// <summary>
        /// 配置表列名。支持 "列名" 或 "列名=默认值" 两种格式。
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// 默认值。当配置表对应单元格为空时，使用此值作为后备。
        /// </summary>
        public string DefaultValue { get; }

        public ConfigFieldAttribute(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("列名不能为空。", nameof(columnName));

            // 解析 "列名=值" 格式
            int defaultIndex = columnName.IndexOf("=", StringComparison.Ordinal);
            if (defaultIndex >= 0)
            {
                ColumnName = columnName.Substring(0, defaultIndex).Trim();
                DefaultValue = columnName.Substring(defaultIndex + 1).Trim();
            }
            else
            {
                ColumnName = columnName.Trim();
                DefaultValue = null;
            }
        }
    }
}
