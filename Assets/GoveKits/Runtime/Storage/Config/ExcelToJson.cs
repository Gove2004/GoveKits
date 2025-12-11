using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Newtonsoft.Json;


namespace GoveKits.Config
{
    public static class ExcelToJson
    {
        public static void Generate(DataSet dataSet, string excelName, string outputFolder)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                string sheetName = table.TableName;
                if (sheetName.StartsWith("#") || table.Rows.Count < 3) continue;

                // 准备表头信息
                List<string> fieldNames = new List<string>();
                List<string> fieldTypes = new List<string>();

                for (int col = 0; col < table.Columns.Count; col++)
                {
                    string name = table.Rows[0][col].ToString().Trim();
                    string type = table.Rows[1][col].ToString().Trim().ToLower();
                    // 这里不需要判空，因为后续是按索引读数据的，但为了对齐，我们存储所有列信息
                    // 如果 name 为空，后续处理时跳过该列即可
                    fieldNames.Add(name);
                    fieldTypes.Add(type);
                }

                // 结果字典: Key(ID) -> Value(RowObject)
                // 使用 object 作为 Key 是为了兼容 int id 和 string id
                var resultDict = new Dictionary<object, Dictionary<string, object>>();

                // 从第3行开始读取数据 (Row Index 2)
                for (int row = 2; row < table.Rows.Count; row++)
                {
                    DataRow dataRow = table.Rows[row];
                    
                    // 获取 ID (默认第一列)
                    string idStr = dataRow[0].ToString().Trim();
                    if (string.IsNullOrEmpty(idStr)) continue; // 跳过空行

                    // 解析 ID 值 (用于 Json 的 Key)
                    object idValue = ParseValue(idStr, fieldTypes[0]);
                    
                    if (resultDict.ContainsKey(idValue))
                    {
                        DebugLogger.LogWarning("Json", $"重复的ID: {idValue} 在表 {sheetName} 中，已跳过。");
                        continue;
                    }

                    // 解析整行数据
                    var rowDict = new Dictionary<string, object>();
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        string fName = fieldNames[col];
                        if (string.IsNullOrEmpty(fName) || fName.StartsWith("#")) continue;

                        string valStr = dataRow[col].ToString().Trim();
                        object valObj = ParseValue(valStr, fieldTypes[col]);
                        rowDict[fName] = valObj;
                    }

                    resultDict.Add(idValue, rowDict);
                }

                // 序列化
                string jsonFileName = $"{excelName}_{sheetName}";
                string jsonContent = JsonConvert.SerializeObject(resultDict, Formatting.Indented);
                string filePath = Path.Combine(outputFolder, $"{jsonFileName}.json");
                
                File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
                DebugLogger.LogGreen("Json", $"Generated: {jsonFileName}.json");
            }
        }

        private static object ParseValue(string value, string type)
        {
            // 处理空值默认值
            if (string.IsNullOrEmpty(value))
            {
                if (type == "int" || type == "long") return 0;
                if (type == "float" || type == "double") return 0.0f;
                if (type == "bool") return false;
                if (type.EndsWith("[]")) return new object[0]; // 空数组
                return "";
            }

            try
            {
                switch (type)
                {
                    case "int": return int.Parse(value);
                    case "long": return long.Parse(value);
                    case "float": return float.Parse(value);
                    case "double": return double.Parse(value);
                    case "bool": 
                        return (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
                    case "string": return value;
                    case "int[]": return Array.ConvertAll(value.Split(','), int.Parse);
                    case "float[]": return Array.ConvertAll(value.Split(','), float.Parse);
                    case "string[]": return value.Split(',');
                    default: return value;
                }
            }
            catch
            {
                DebugLogger.LogError("Json", $"解析失败: 值[{value}] 类型[{type}]");
                return value; // 容错返回原字符串
            }
        }
    }
}