using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace GoveKits.Binary
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class GenBinaryDataAttribute : Attribute 
    { 
        public const string DefaultSavePath = "Scripts/GeneratedCode";
        public string SavePath { get; }
        public GenBinaryDataAttribute(string savePath = DefaultSavePath) => SavePath = savePath;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BinaryMemberAttribute : Attribute
    {
        public readonly ushort Tag;
        public BinaryMemberAttribute(ushort tag) => Tag = tag;
    }

    public enum WireType : byte
    {
        Fixed1 = 0,   // Bool, Byte
        Fixed4 = 1,   // Int, Float
        Fixed8 = 2,   // Long, Double, Vector2
        Fixed12 = 3,  // Vector3
        Fixed16 = 4,  // Vector4, Quaternion, Rect, Color
        String = 5,   // String, Bytes, NestedObject, Lists (Length-Delimited)
    }


    
    /// <summary>
    /// 核心代码生成器：负责解析 Type 并生成 C# 源代码字符串。
    /// 适配新的 IBinaryData 接口 (Reading 带 endPos)。
    /// </summary>
    public static class BinaryCodeBuilder
    {
        public static string Build(Type type)
        {
            // === 1. 继承关系分析 ===
            // 判断是否是子类（父类也标记了 GenBinaryData）
            bool isChild = type.BaseType != null && 
                           type.BaseType.IsDefined(typeof(GenBinaryDataAttribute), false);

            string interfaceDecl = isChild ? "" : " : IBinaryData"; 
            string modifier = isChild ? "override" : "virtual";

            // === 2. 字段解析 ===
            
            // DeclaredOnly: 仅当前类定义的字段 (用于 Writing 和 Length 的增量计算)
            var declaredMembers = GetMembers(type, BindingFlags.DeclaredOnly);

            // All: 所有字段 (包括父类) (用于 Reading 的全量解析)
            // 注意：Reading 必须重写并处理所有字段，因为父类的 Reading 会跳过子类未知的 Tag，导致无法继续读取。
            var allMembers = GetMembers(type, BindingFlags.FlattenHierarchy);

            // === 3. 代码构建 ===
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using GoveKits.Binary;");
            sb.AppendLine($"using UnityEngine;");
            sb.AppendLine($"");

            // Namespace
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                sb.AppendLine($"namespace {type.Namespace}");
                sb.AppendLine("{");
            }
            
            sb.AppendLine($"    // Generated Code. Do not modify.");
            sb.AppendLine($"    public partial class {type.Name}{interfaceDecl}");
            sb.AppendLine($"    {{");

            // --- Length() ---
            sb.AppendLine($"        public {modifier} int Length()");
            sb.AppendLine("        {");
            // 如果是子类，先获取 base.Length()，否则从 0 开始
            sb.AppendLine(isChild ? "            int total = base.Length();" : "            int total = 0;");
            
            foreach (var item in declaredMembers)
                sb.AppendLine($"            total += BinaryLengthHelper.Get({item.Name});");
            
            sb.AppendLine("            return total;");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // --- Writing() ---
            sb.AppendLine($"        public {modifier} void Writing(byte[] buffer, ref int index)");
            sb.AppendLine("        {");
            // 先写父类数据
            if (isChild) sb.AppendLine("            base.Writing(buffer, ref index);");
            
            foreach (var item in declaredMembers)
                sb.AppendLine($"            BinaryWriteHelper.Write(buffer, ref index, {item.Tag}, {item.Name});");
            
            sb.AppendLine("        }");
            sb.AppendLine("");

            // --- Reading() (核心适配部分) ---
            // 签名更新：增加 endPos 参数
            sb.AppendLine($"        public {modifier} void Reading(byte[] buffer, ref int index, int endPos)");
            sb.AppendLine("        {");
            
            // 重要：子类不能调用 base.Reading，因为 base.Reading 会把子类的 Tag 当作未知数据跳过。
            // 因此这里生成一个处理所有字段 (allMembers) 的大循环。
            
            sb.AppendLine("            while (index < endPos)");
            sb.AppendLine("            {");
            // 传入 endPos 给 ReadHeader 做越界检查
            sb.AppendLine("                if (!BinaryReadHelper.ReadHeader(buffer, ref index, endPos, out ushort tag, out WireType type)) break;");
            sb.AppendLine("                switch (tag)");
            sb.AppendLine("                {");
            
            foreach (var item in allMembers)
                sb.AppendLine($"                    case {item.Tag}: BinaryReadHelper.Read(buffer, ref index, out {item.Name}); break;");
            
            sb.AppendLine("                    default: BinaryReadHelper.Skip(buffer, ref index, type); break;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");

            sb.AppendLine($"    }}");
            if (!string.IsNullOrEmpty(type.Namespace)) sb.AppendLine("}");

            return sb.ToString();
        }

        // 辅助方法：获取排序后的字段信息
        private static List<MemberInfoData> GetMembers(Type type, BindingFlags extraFlags)
        {
            // BindingFlags 组合：Public/NonPublic + Instance + (DeclaredOnly or FlattenHierarchy)
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | extraFlags;
            
            var members = type.GetMembers(flags)
                .Where(m => m.IsDefined(typeof(BinaryMemberAttribute), false))
                .Select(m => new MemberInfoData 
                { 
                    Name = m.Name, 
                    Tag = m.GetCustomAttribute<BinaryMemberAttribute>().Tag 
                })
                .OrderBy(x => x.Tag) // 必须按 Tag 排序
                .ToList();
            
            return members;
        }

        private struct MemberInfoData
        {
            public string Name;
            public ushort Tag;
        }
    }
}