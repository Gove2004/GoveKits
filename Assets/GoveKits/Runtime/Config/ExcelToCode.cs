using System.Data;
using System.IO;
using System.Text;


namespace GoveKits.Config
{
    /// <summary>
    /// 配置数据接口标记
    /// </summary>
    public interface IConfigData { }


    public static class ExcelToCode
    {
        public static void Generate(DataSet dataSet, string excelName, string outputFolder, string nameSpace)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                string sheetName = table.TableName;
                // 跳过规则：#开头 或 行数不足3行
                if (sheetName.StartsWith("#") || table.Rows.Count < 3) continue;

                string className = $"{excelName}_{sheetName}Config";
                StringBuilder sb = new StringBuilder();

                // 头部引用
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using GoveKits.Config;"); // 引用基础接口
                sb.AppendLine();
                sb.AppendLine($"namespace {nameSpace}");
                sb.AppendLine("{");
                sb.AppendLine("    [Serializable]");
                sb.AppendLine($"    public class {className} : IConfigData");
                sb.AppendLine("    {");

                // 解析列
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    // Row 0: 字段名 (Id, Name, Atk)
                    // Row 1: 类型 (int, string, float)
                    string fieldName = table.Rows[0][col].ToString().Trim();
                    string fieldType = table.Rows[1][col].ToString().Trim().ToLower();

                    // 跳过空列或注释列
                    if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("#")) continue;

                    string csharpType = MapType(fieldType);
                    sb.AppendLine($"        public {csharpType} {fieldName};");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                string filePath = Path.Combine(outputFolder, $"{className}.Gen.cs");
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                DebugLogger.LogGreen("Code", $"Generated: {className}.Gen.cs");
            }
        }

        private static string MapType(string excelType)
        {
            switch (excelType)
            {
                case "int": return "int";
                case "float": return "float";
                case "double": return "double";
                case "bool": return "bool";
                case "string": return "string";
                case "long": return "long";
                case "int[]": return "int[]";
                case "string[]": return "string[]";
                case "float[]": return "float[]";
                default: return "string"; // 默认回退到 string，防止报错
            }
        }
    }
}