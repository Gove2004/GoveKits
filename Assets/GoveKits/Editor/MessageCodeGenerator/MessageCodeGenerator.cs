using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using GoveKits.Network;
using GoveKits.Save;

namespace GoveKits.Tool
{
    public class MessageGeneratorWindow : EditorWindow
    {
        // 默认路径
        private const string DEFAULT_PATH = "Assets/Scripts/Messages/Generated";
        private const string PREF_KEY_PATH = "GoveKits_MsgGen_Path";

        [SerializeField] private string savePath = DEFAULT_PATH;

        [MenuItem("GoveKits/Message Generator")]
        public static void ShowWindow()
        {
            GetWindow<MessageGeneratorWindow>("Msg Gen");
        }

        private void OnEnable()
        {
            // 加载上次保存的路径
            savePath = EditorPrefs.GetString(PREF_KEY_PATH, DEFAULT_PATH);
        }

        private void OnDisable()
        {
            // 保存路径配置
            EditorPrefs.SetString(PREF_KEY_PATH, savePath);
        }

        private void OnGUI()
        {
            GUILayout.Label("网络消息代码生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 路径配置
            GUILayout.Label("生成路径 (相对于项目根目录):");
            GUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string path = EditorUtility.OpenFolderPanel("选择生成目录", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为相对路径
                    if (path.StartsWith(Application.dataPath))
                    {
                        savePath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        savePath = path; // 如果选了外部路径
                    }
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            GUILayout.Label("操作", EditorStyles.boldLabel);

            // 1. 清空按钮 (红色警示风格)
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("1. 清空旧文件 (Clean)", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("警告", $"确定要清空以下目录吗？\n{savePath}", "确定清空", "取消"))
                {
                    ClearGeneratedFiles();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 2. 生成按钮 (绿色强调风格)
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("2. 生成消息代码 (Generate)", GUILayout.Height(45)))
            {
                GenerateMessages();
            }
            GUI.backgroundColor = Color.white;
        }

        // ========================================================================
        // 核心逻辑
        // ========================================================================

        private void ClearGeneratedFiles()
        {
            if (!Directory.Exists(savePath)) return;

            string[] files = Directory.GetFiles(savePath, "*Msg.cs"); // 只删 *Msg.cs 防止误删
            foreach (var file in files)
            {
                File.Delete(file);
            }
            
            // 如果有 .meta 文件也清理一下
            AssetDatabase.Refresh();
            Debug.Log($"[MessageGen] 已清空目录: {savePath}");
        }

        private void GenerateMessages()
        {
            // 1. 扫描所有程序集，找到带有 [AutoMessage] 的类
            // 注意：这里需要确保 AutoMessageAttribute 是在你的 Runtime 程序集中定义的
            var schemaTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<AutoMessageAttribute>() != null)
                .ToArray();

            if (schemaTypes.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到带有 [AutoMessage] 特性的类。\n请检查 Schema 类定义。", "OK");
                return;
            }

            // 确保目录存在
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            int count = 0;
            int errorCount = 0;

            foreach (var type in schemaTypes)
            {
                try
                {
                    GenerateMessageClass(type, savePath);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"生成 {type.Name} 失败: {e.Message}");
                    errorCount++;
                }
            }

            AssetDatabase.Refresh();

            string resultMsg = $"生成完成！\n\n成功: {count} 个\n失败: {errorCount} 个\n路径: {savePath}";
            EditorUtility.DisplayDialog("生成报告", resultMsg, "OK");
            Debug.Log($"[MessageGen] Generated {count} files.");
        }

        private static void GenerateMessageClass(Type schemaType, string dirPath)
        {
            var attr = schemaType.GetCustomAttribute<AutoMessageAttribute>();
            
            // 获取该类的所有公开字段，排除 [AutoMessageIgnoreField] 标记的字段
            var fields = schemaType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<AutoMessageIgnoreFieldAttribute>() == null)
                .ToArray();

            string className = schemaType.Name + "Msg"; 
            string namespaceName = schemaType.Namespace; 

            StringBuilder sb = new StringBuilder();

            // ================== 头文件 ==================
            sb.AppendLine("// ========================================================");
            sb.AppendLine("// AUTO GENERATED CODE - DO NOT EDIT DIRECTLY");
            sb.AppendLine($"// Generated from: {schemaType.FullName}");
            sb.AppendLine("// ========================================================");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using GoveKits.Save;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using GoveKits.Network;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            // ================== 类定义 ==================
            sb.AppendLine($"    [Message({attr.Id})]"); 
            sb.AppendLine($"    public partial class {className} : Message");
            sb.AppendLine("    {");

            // ================== 字段定义 ==================
            foreach (var f in fields)
            {
                string typeName = GetFriendlyTypeName(f.FieldType);
                sb.AppendLine($"        public {typeName} {f.Name};");
            }
            sb.AppendLine();

            // ================== BodyLength ==================
            sb.AppendLine("        protected override int BodyLength()");
            sb.AppendLine("        {");
            sb.AppendLine("            int len = 0;");
            foreach (var f in fields) sb.AppendLine(GetLengthCode(f));
            sb.AppendLine("            return len;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // ================== BodyWriting ==================
            sb.AppendLine("        protected override void BodyWriting(byte[] buffer, ref int index)");
            sb.AppendLine("        {");
            foreach (var f in fields) sb.AppendLine(GetWriteCode(f));
            sb.AppendLine("        }");
            sb.AppendLine();

            // ================== BodyReading ==================
            sb.AppendLine("        protected override void BodyReading(byte[] buffer, ref int index)");
            sb.AppendLine("        {");
            foreach (var f in fields) sb.AppendLine(GetReadCode(f));
            sb.AppendLine("        }");

            sb.AppendLine("    }"); // End Class

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}"); // End Namespace
            }

            // 写入文件
            string filePath = Path.Combine(dirPath, className + ".cs");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // ========================================================================
        // 类型映射与代码片段生成 (保持原逻辑不变)
        // ========================================================================

        private static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(long)) return "long";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(string)) return "string";
            if (type == typeof(byte[])) return "byte[]";
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            // 简单处理 List，如果需要支持 List<T> 需要更复杂的逻辑，这里暂时按原逻辑返回 FullName
            return type.FullName.Replace('+', '.'); // 处理嵌套类
        }

        private static string GetLengthCode(FieldInfo f)
        {
            Type t = f.FieldType;
            string name = f.Name;

            if (t == typeof(int) || t == typeof(float)) return $"            len += 4; // {name}";
            if (t == typeof(long) || t == typeof(double)) return $"            len += 8; // {name}";
            if (t == typeof(short)) return $"            len += 2; // {name}";
            if (t == typeof(byte) || t == typeof(bool)) return $"            len += 1; // {name}";
            if (t == typeof(Vector2)) return $"            len += 8; // {name}";
            if (t == typeof(Vector3)) return $"            len += 12; // {name}";

            if (t == typeof(string))
                return $"            len += 4 + (string.IsNullOrEmpty({name}) ? 0 : Encoding.UTF8.GetByteCount({name}));";

            if (t == typeof(byte[]))
                return $"            len += 4 + ({name} == null ? 0 : {name}.Length);";

            if (typeof(IBinaryData).IsAssignableFrom(t))
                return $"            len += {name} == null ? 0 : {name}.Length();";

            if (t.IsEnum) return $"            len += 4; // {name} (Enum)";

            return $"            // Error: Unsupported type {t.Name}";
        }

        private static string GetWriteCode(FieldInfo f)
        {
            Type t = f.FieldType;
            string name = f.Name;

            if (t == typeof(int)) return $"            WriteInt(buffer, {name}, ref index);";
            if (t == typeof(float)) return $"            WriteFloat(buffer, {name}, ref index);";
            if (t == typeof(long)) return $"            WriteLong(buffer, {name}, ref index);";
            if (t == typeof(short)) return $"            WriteShort(buffer, {name}, ref index);";
            if (t == typeof(byte)) return $"            WriteByte(buffer, {name}, ref index);";
            if (t == typeof(bool)) return $"            WriteBool(buffer, {name}, ref index);";
            if (t == typeof(string)) return $"            WriteString(buffer, {name}, ref index);";
            if (t == typeof(byte[])) return $"            WriteBytes(buffer, {name}, ref index);";
            if (t == typeof(Vector2)) return $"            WriteVector2(buffer, {name}, ref index);";
            if (t == typeof(Vector3)) return $"            WriteVector3(buffer, {name}, ref index);";

            if (typeof(IBinaryData).IsAssignableFrom(t))
                return $"            WriteData(buffer, {name}, ref index);";
            
            if (t.IsEnum) return $"            WriteInt(buffer, (int){name}, ref index);";

            return $"            // Error: Can't write {name}";
        }

        private static string GetReadCode(FieldInfo f)
        {
            Type t = f.FieldType;
            string name = f.Name;

            if (t == typeof(int)) return $"            {name} = ReadInt(buffer, ref index);";
            if (t == typeof(float)) return $"            {name} = ReadFloat(buffer, ref index);";
            if (t == typeof(long)) return $"            {name} = ReadLong(buffer, ref index);";
            if (t == typeof(short)) return $"            {name} = ReadShort(buffer, ref index);";
            if (t == typeof(byte)) return $"            {name} = ReadByte(buffer, ref index);";
            if (t == typeof(bool)) return $"            {name} = ReadBool(buffer, ref index);";
            if (t == typeof(string)) return $"            {name} = ReadString(buffer, ref index);";
            if (t == typeof(byte[])) return $"            {name} = ReadBytes(buffer, ref index);";
            if (t == typeof(Vector2)) return $"            {name} = ReadVector2(buffer, ref index);";
            if (t == typeof(Vector3)) return $"            {name} = ReadVector3(buffer, ref index);";

            if (typeof(IBinaryData).IsAssignableFrom(t))
            {
                // 注意：这里需要处理全名，防止嵌套类冲突
                string typeName = t.FullName.Replace('+', '.');
                return $"            {name} = ReadData<{typeName}>(buffer, ref index);";
            }

            if (t.IsEnum) 
            {
                string typeName = t.FullName.Replace('+', '.');
                return $"            {name} = ({typeName})ReadInt(buffer, ref index);";
            }

            return $"            // Error: Can't read {name}";
        }
    }
}