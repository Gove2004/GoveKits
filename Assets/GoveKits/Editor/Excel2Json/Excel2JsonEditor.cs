using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Tool
{
    public class ExcelConfigEditor : EditorWindow
    {
        [Header("路径配置")]
        [SerializeField] private string excelFolderPath = "Assets/Config/Excel";
        [SerializeField] private string codeOutputFolder = "Assets/Config/DTO"; 
        [SerializeField] private string jsonOutputFolder = "Assets/Config/Json"; 
        [SerializeField] private string namespaceName = "GoveKits.Config"; // 必须与Manager中引用的一致

        [MenuItem("GoveKits/Excel2Json")]
        public static void ShowWindow()
        {
            GetWindow<ExcelConfigEditor>("Excel2Json");
        }

        private void OnGUI()
        {
            GUILayout.Label("Excel 导表工具", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            excelFolderPath = EditorGUILayout.TextField("Excel 目录", excelFolderPath);
            codeOutputFolder = EditorGUILayout.TextField("DTO 代码目录", codeOutputFolder);
            jsonOutputFolder = EditorGUILayout.TextField("JSON 数据目录", jsonOutputFolder);
            namespaceName = EditorGUILayout.TextField("命名空间", namespaceName);

            EditorGUILayout.Space(20);
            GUILayout.Label("操作流程", EditorStyles.boldLabel);

            // 1. 清空
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("1. 清空旧文件 (Clean)", GUILayout.Height(30)))
            {
                if(EditorUtility.DisplayDialog("警告", "确定要清空 DTO 和 Json 文件夹下的所有生成文件吗？", "确定", "取消"))
                {
                    ClearFolders();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // 2. 分步生成
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("2. 仅生成 C# 代码", GUILayout.Height(30)))
            {
                GenerateProcess(true, false);
            }
            if (GUILayout.Button("3. 仅生成 JSON 数据", GUILayout.Height(30)))
            {
                GenerateProcess(false, true);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 3. 一键生成
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("一键全部生成 (Generate All)", GUILayout.Height(40)))
            {
                GenerateProcess(true, true);
            }
            GUI.backgroundColor = Color.white;
        }

        private void ClearFolders()
        {
            void CleanDir(string path, string pattern)
            {
                if (!Directory.Exists(path)) return;
                string[] files = Directory.GetFiles(path, pattern);
                foreach (var file in files) File.Delete(file);
                Debug.Log($"[已删除] {path}/{pattern}");
            }

            CleanDir(codeOutputFolder, "*.cs");
            CleanDir(jsonOutputFolder, "*.json");
            AssetDatabase.Refresh();
        }

        private void GenerateProcess(bool genCode, bool genJson)
        {
            if (!Directory.Exists(excelFolderPath))
            {
                Debug.LogError("Excel目录不存在！");
                return;
            }

            if (genCode)
            {
                if (!Directory.Exists(codeOutputFolder)) Directory.CreateDirectory(codeOutputFolder);
            }
            if (genJson)
            {
                if (!Directory.Exists(jsonOutputFolder)) Directory.CreateDirectory(jsonOutputFolder);
            }

            string[] files = Directory.GetFiles(excelFolderPath, "*.xlsx");
            int count = 0;

            foreach (string filePath in files)
            {
                if (Path.GetFileName(filePath).StartsWith("~$")) continue;

                try
                {
                    ProcessFile(filePath, genCode, genJson);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"文件出错 {filePath}: {e.Message}");
                }
            }

            AssetDatabase.Refresh();
            string msg = $"处理完成！({count} 个文件)\n";
            if (genCode) msg += "- 代码已更新 (需等待编译)\n";
            if (genJson) msg += "- 数据已更新";
            EditorUtility.DisplayDialog("完成", msg, "OK");
        }

        private void ProcessFile(string filePath, bool genCode, bool genJson)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    string excelName = Path.GetFileNameWithoutExtension(filePath);

                    foreach (DataTable table in result.Tables)
                    {
                        string sheetName = table.TableName;
                        // 跳过注释页(#开头)或行数不足的页
                        if (sheetName.StartsWith("#") || table.Rows.Count < 3) continue;

                        string finalName = $"{excelName}_{sheetName}"; 
                        string className = $"{finalName}Config";

                        // 解析表头
                        List<string> fieldNames = new List<string>();
                        List<string> fieldTypes = new List<string>();

                        for (int col = 0; col < table.Columns.Count; col++)
                        {
                            string fieldName = table.Rows[0][col].ToString().Trim();
                            string fieldType = table.Rows[1][col].ToString().Trim().ToLower();
                            if (string.IsNullOrEmpty(fieldName)) continue;
                            fieldNames.Add(fieldName);
                            fieldTypes.Add(fieldType);
                        }

                        if (genCode) GenerateCSharpClass(className, fieldNames, fieldTypes);
                        if (genJson) GenerateJsonData(finalName, table, fieldNames, fieldTypes);
                    }
                }
            }
        }

        private void GenerateCSharpClass(string className, List<string> names, List<string> types)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {className} : IConfigData"); 
            sb.AppendLine("    {");

            for (int i = 0; i < names.Count; i++)
            {
                sb.AppendLine($"        public {MapTypeToCSharp(types[i])} {names[i]};"); 
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(codeOutputFolder, $"{className}.cs"), sb.ToString(), Encoding.UTF8);
            Debug.Log($"[Code] {className}.cs");
        }

        private void GenerateJsonData(string fileName, DataTable table, List<string> names, List<string> types)
        {
            var resultDict = new Dictionary<object, Dictionary<string, object>>();
            string keyType = types[0]; 

            for (int row = 2; row < table.Rows.Count; row++)
            {
                DataRow dataRow = table.Rows[row];
                string idStr = dataRow[0].ToString();
                if (string.IsNullOrEmpty(idStr)) continue;

                object idValue = ParseValue(idStr, keyType);
                var rowDict = new Dictionary<string, object>();
                
                for (int col = 0; col < names.Count; col++)
                {
                    rowDict[names[col]] = ParseValue(dataRow[col].ToString(), types[col]);
                }

                if (!resultDict.ContainsKey(idValue)) resultDict.Add(idValue, rowDict);
            }

            string json = JsonConvert.SerializeObject(resultDict, Formatting.Indented);
            File.WriteAllText(Path.Combine(jsonOutputFolder, $"{fileName}.json"), json, Encoding.UTF8);
            Debug.Log($"[Json] {fileName}.json");
        }

        private string MapTypeToCSharp(string excelType)
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
                default: return "string";
            }
        }

        private object ParseValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (type == "int" || type == "float" || type == "double") return 0;
                if (type == "bool") return false;
                return "";
            }
            try
            {
                switch (type)
                {
                    case "int": return int.Parse(value);
                    case "float": return float.Parse(value);
                    case "double": return double.Parse(value);
                    case "long": return long.Parse(value);
                    case "bool": return (value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase));
                    case "string": return value;
                    case "int[]": return Array.ConvertAll(value.Split(','), int.Parse);
                    case "string[]": return value.Split(',');
                    default: return value;
                }
            }
            catch { return value; }
        }
    }
}