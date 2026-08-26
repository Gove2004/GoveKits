using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// CSV 配置解析器。支持通过 [ConfigField] 特性指定列别名和默认值，
    /// 自动处理类型转换、枚举解析和空值回退。
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

            var rows = new List<T>();
            if (string.IsNullOrWhiteSpace(csv)) return rows;

            string[] lines = csv.Replace("\r\n", "\n").Split('\n');
            if (lines.Length <= 1) return rows;

            string[] headers = SplitCsvLine(lines[0]);
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

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = SplitCsvLine(lines[i]);
                var item = new T();

                // 遍历所有字段，优先使用 [ConfigField] 指定的列名
                for (int f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];
                    string colName = GetConfigFieldName(field);
                    int colIdx;

                    if (!string.IsNullOrEmpty(colName) && headerIndex.TryGetValue(colName, out colIdx))
                    {
                        SetFieldValue(item, field, values, colIdx, GetFieldDefaultValue(field));
                    }
                }

                // 遍历所有属性
                for (int p = 0; p < props.Length; p++)
                {
                    var prop = props[p];
                    if (!prop.CanWrite) continue;

                    string colName = GetConfigFieldName(prop);
                    int colIdx;

                    if (!string.IsNullOrEmpty(colName) && headerIndex.TryGetValue(colName, out colIdx))
                    {
                        SetPropertyValue(item, prop, values, colIdx, GetPropertyDefaultValue(prop));
                    }
                }

                rows.Add(item);
            }

            return rows;
        }

        private static string GetConfigFieldName(MemberInfo member)
        {
            var attr = member.GetCustomAttribute<ConfigFieldAttribute>(false);
            return attr?.ColumnName;
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

            object converted = ConvertTo(raw, field.FieldType);
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

            object converted = ConvertTo(raw, prop.PropertyType);
            if (converted != null)
                prop.SetValue(item, converted);
        }

        private static string[] SplitCsvLine(string line)
        {
            var values = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(ch);
            }

            values.Add(sb.ToString());
            return values.ToArray();
        }

        private static object ConvertTo(string raw, Type targetType)
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
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
    }
}
