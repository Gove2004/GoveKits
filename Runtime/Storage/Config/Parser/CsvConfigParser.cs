using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// CSV 配置解析器。支持通过 [ConfigField] 特性指定列别名和默认值（未标注时回退字段名匹配），
    /// 自动处理类型转换、枚举解析和空值回退，并正确解析引号内含逗号/换行的字段。
    /// </summary>
    /// <remarks>
    /// 第一行必须为表头，后续每一行按列别名（或字段名）映射到目标类型的字段和属性。
    /// </remarks>
    public sealed class CsvConfigParser : IConfigParser
    {
        private static readonly string[] ParserExtensions = { "csv" };

        public IReadOnlyList<string> Extensions => ParserExtensions;

        public List<T> Parse<T>(byte[] bytes, string text) where T : class, IConfigData, new()
        {
            string csv = string.IsNullOrEmpty(text)
                ? Encoding.UTF8.GetString(bytes)
                : text;

            // 去除 UTF-8 BOM，避免污染首个表头键
            csv = csv.TrimStart('\uFEFF');

            var rows = new List<T>();
            if (string.IsNullOrWhiteSpace(csv)) return rows;

            // 整体解析为记录（正确处理引号内的逗号与换行）
            var records = ParseCsvRecords(csv);
            if (records.Count <= 1) return rows;

            string[] headers = records[0];
            FieldInfo[] fields = typeof(T).GetFields();
            PropertyInfo[] props = typeof(T).GetProperties();

            // 构建列名到索引的映射
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h))
                    headerIndex[h] = i;
            }

            for (int i = 1; i < records.Count; i++)
            {
                string[] values = records[i];
                if (IsBlankRecord(values)) continue;

                var item = new T();

                // 遍历所有字段，优先使用 [ConfigField] 指定的列名，否则回退字段名
                for (int f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];
                    string colName = GetConfigFieldName(field, field.Name);

                    if (headerIndex.TryGetValue(colName, out int colIdx))
                    {
                        SetFieldValue(item, field, values, colIdx, GetFieldDefaultValue(field));
                    }
                }

                // 遍历所有属性
                for (int p = 0; p < props.Length; p++)
                {
                    var prop = props[p];
                    if (!prop.CanWrite) continue;

                    string colName = GetConfigFieldName(prop, prop.Name);

                    if (headerIndex.TryGetValue(colName, out int colIdx))
                    {
                        SetPropertyValue(item, prop, values, colIdx, GetPropertyDefaultValue(prop));
                    }
                }

                rows.Add(item);
            }

            return rows;
        }

        /// <summary>
        /// 将整个 CSV 文本解析为记录列表。使用逐字符状态机，
        /// 引号内的逗号、换行和转义的双引号（""）都会被正确保留。
        /// </summary>
        private static List<string[]> ParseCsvRecords(string csv)
        {
            var records = new List<string[]>();
            var values = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            bool recordStarted = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char ch = csv[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        // 引号内的换行、逗号原样保留
                        sb.Append(ch);
                    }
                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                    recordStarted = true;
                }
                else if (ch == ',')
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                    recordStarted = true;
                }
                else if (ch == '\n' || ch == '\r')
                {
                    // \r\n 视为单个换行
                    if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;

                    if (recordStarted || sb.Length > 0 || values.Count > 0)
                    {
                        values.Add(sb.ToString());
                        sb.Clear();
                        records.Add(values.ToArray());
                        values.Clear();
                        recordStarted = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                    recordStarted = true;
                }
            }

            if (recordStarted || sb.Length > 0 || values.Count > 0)
            {
                values.Add(sb.ToString());
                records.Add(values.ToArray());
            }

            return records;
        }

        private static bool IsBlankRecord(string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i])) return false;
            }
            return true;
        }

        private static string GetConfigFieldName(MemberInfo member, string fallbackName)
        {
            var attr = member.GetCustomAttribute<ConfigFieldAttribute>(false);
            return string.IsNullOrEmpty(attr?.ColumnName) ? fallbackName : attr.ColumnName;
        }

        private static string GetFieldDefaultValue(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<ConfigFieldAttribute>(false);
            return attr?.DefaultValue;
        }

        private static string GetPropertyDefaultValue(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<ConfigFieldAttribute>(false);
            return attr?.DefaultValue;
        }

        private static void SetFieldValue(object item, FieldInfo field, string[] values, int colIdx, string defaultVal)
        {
            if (colIdx >= values.Length) return;

            string raw = values[colIdx]?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                if (!string.IsNullOrEmpty(defaultVal))
                    raw = defaultVal;
                else
                    return;
            }

            object converted = ConvertTo(raw, field.FieldType, field.Name);
            if (converted != null)
                field.SetValue(item, converted);
        }

        private static void SetPropertyValue(object item, PropertyInfo prop, string[] values, int colIdx, string defaultVal)
        {
            if (colIdx >= values.Length) return;

            string raw = values[colIdx]?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                if (!string.IsNullOrEmpty(defaultVal))
                    raw = defaultVal;
                else
                    return;
            }

            object converted = ConvertTo(raw, prop.PropertyType, prop.Name);
            if (converted != null)
                prop.SetValue(item, converted);
        }

        private static object ConvertTo(string raw, Type targetType, string memberName)
        {
            if (targetType == typeof(string))
                return raw ?? string.Empty;

            if (string.IsNullOrEmpty(raw))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            Type realType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (realType.IsEnum)
                    return Enum.Parse(realType, raw, true);

                if (realType == typeof(bool))
                {
                    if (raw == "1") return true;
                    if (raw == "0") return false;
                }

                return Convert.ChangeType(raw, realType);
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(CsvConfigParser),
                    $"字段 {memberName} 的值 \"{raw}\" 无法转换为 {realType.Name}: {e.Message}");
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
    }
}
