using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// Csv 配置解析器。
    /// </summary>
    /// <remarks>
    /// 第一行必须是表头，后续按字段名/属性名（忽略大小写）进行映射。
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
            if (string.IsNullOrWhiteSpace(csv))
            {
                return rows;
            }

            string[] lines = csv.Replace("\r\n", "\n").Split('\n');
            if (lines.Length <= 1)
            {
                return rows;
            }

            string[] headers = SplitCsvLine(lines[0]);
            FieldInfo[] fields = typeof(T).GetFields();
            PropertyInfo[] props = typeof(T).GetProperties();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] values = SplitCsvLine(lines[i]);
                var item = new T();
                for (int c = 0; c < headers.Length && c < values.Length; c++)
                {
                    string header = headers[c].Trim();
                    if (string.IsNullOrEmpty(header))
                    {
                        continue;
                    }

                    string raw = values[c];

                    FieldInfo field = Array.Find(fields, f => string.Equals(f.Name, header, StringComparison.OrdinalIgnoreCase));
                    if (field != null)
                    {
                        object converted = ConvertTo(raw, field.FieldType);
                        if (converted != null)
                        {
                            field.SetValue(item, converted);
                        }

                        continue;
                    }

                    PropertyInfo prop = Array.Find(props, p => string.Equals(p.Name, header, StringComparison.OrdinalIgnoreCase) && p.CanWrite);
                    if (prop != null)
                    {
                        object converted = ConvertTo(raw, prop.PropertyType);
                        if (converted != null)
                        {
                            prop.SetValue(item, converted);
                        }
                    }
                }

                rows.Add(item);
            }

            return rows;
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
            {
                return raw ?? string.Empty;
            }

            if (string.IsNullOrEmpty(raw))
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            Type realType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (realType.IsEnum)
                {
                    return Enum.Parse(realType, raw, true);
                }

                if (realType == typeof(bool))
                {
                    // 兼容常见配置写法: 1/0。
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
