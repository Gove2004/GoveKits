using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GoveKits.Binary
{
    public static class BinaryWriteHelper
    {
        // --- Internal Basics ---
        private static void WriteHeader(byte[] buffer, ref int index, ushort tag, WireType type)
        {
            buffer[index++] = (byte)tag;
            buffer[index++] = (byte)(tag >> 8);
            buffer[index++] = (byte)type;
        }

        // 小端序写入 Int
        private static void WriteRawInt(byte[] buffer, ref int index, int val)
        {
            buffer[index++] = (byte)val;
            buffer[index++] = (byte)(val >> 8);
            buffer[index++] = (byte)(val >> 16);
            buffer[index++] = (byte)(val >> 24);
        }

        // --- Primitives ---
        public static void Write(byte[] buffer, ref int index, ushort tag, int val)
        {
            WriteHeader(buffer, ref index, tag, WireType.Fixed4);
            WriteRawInt(buffer, ref index, val);
        }

        public static void Write(byte[] buffer, ref int index, ushort tag, float val)
        {
            WriteHeader(buffer, ref index, tag, WireType.Fixed4);
            // 使用 Union 或 BitConverter
            UnionIntFloat u = new UnionIntFloat { FloatValue = val };
            WriteRawInt(buffer, ref index, u.IntValue);
        }

        public static void Write(byte[] buffer, ref int index, ushort tag, bool val)
        {
            WriteHeader(buffer, ref index, tag, WireType.Fixed1);
            buffer[index++] = val ? (byte)1 : (byte)0;
        }

        public static void Write(byte[] buffer, ref int index, ushort tag, long val)
        {
            WriteHeader(buffer, ref index, tag, WireType.Fixed8);
            WriteRawInt(buffer, ref index, (int)val);
            WriteRawInt(buffer, ref index, (int)(val >> 32));
        }

        // --- Unity Types ---
        public static void Write(byte[] buffer, ref int index, ushort tag, Vector3 val)
        {
            WriteHeader(buffer, ref index, tag, WireType.Fixed12);
            UnionIntFloat u = new UnionIntFloat();
            u.FloatValue = val.x; WriteRawInt(buffer, ref index, u.IntValue);
            u.FloatValue = val.y; WriteRawInt(buffer, ref index, u.IntValue);
            u.FloatValue = val.z; WriteRawInt(buffer, ref index, u.IntValue);
        }

        public static void Write(byte[] buffer, ref int index, ushort tag, Quaternion val)
        {
            WriteHeader(buffer, ref index, tag, WireType.Fixed16);
            UnionIntFloat u = new UnionIntFloat();
            u.FloatValue = val.x; WriteRawInt(buffer, ref index, u.IntValue);
            u.FloatValue = val.y; WriteRawInt(buffer, ref index, u.IntValue);
            u.FloatValue = val.z; WriteRawInt(buffer, ref index, u.IntValue);
            u.FloatValue = val.w; WriteRawInt(buffer, ref index, u.IntValue);
        }

        // --- String ---
        public static void Write(byte[] buffer, ref int index, ushort tag, string val)
        {
            WriteHeader(buffer, ref index, tag, WireType.String);
            if (string.IsNullOrEmpty(val))
            {
                WriteRawInt(buffer, ref index, 0);
            }
            else
            {
                int len = Encoding.UTF8.GetByteCount(val);
                WriteRawInt(buffer, ref index, len);
                // 优化：直接写入 buffer，避免分配 new byte[]
                Encoding.UTF8.GetBytes(val, 0, val.Length, buffer, index);
                index += len;
            }
        }

        // --- Nested Object ---
        public static void Write<T>(byte[] buffer, ref int index, ushort tag, T val) where T : IBinaryData
        {
            WriteHeader(buffer, ref index, tag, WireType.String);
            if (val == null)
            {
                WriteRawInt(buffer, ref index, 0);
            }
            else
            {
                WriteRawInt(buffer, ref index, val.Length());
                val.Writing(buffer, ref index);
            }
        }

        // --- Lists ---
        // 统一逻辑：先写 TotalLen (包含Count)，再写 Count，再写内容
        
        public static void Write(byte[] buffer, ref int index, ushort tag, List<int> val)
        {
            WriteHeader(buffer, ref index, tag, WireType.String);
            if (val == null) { WriteRawInt(buffer, ref index, 0); return; }

            int count = val.Count;
            int bodyLen = 4 + count * 4; // Count(4) + Data
            
            WriteRawInt(buffer, ref index, bodyLen);
            WriteRawInt(buffer, ref index, count);
            for (int i = 0; i < count; i++) WriteRawInt(buffer, ref index, val[i]);
        }

        public static void Write(byte[] buffer, ref int index, ushort tag, List<byte> val)
        {
            WriteHeader(buffer, ref index, tag, WireType.String);
            if (val == null) { WriteRawInt(buffer, ref index, 0); return; }

            int count = val.Count;
            int bodyLen = 4 + count; 
            
            WriteRawInt(buffer, ref index, bodyLen);
            WriteRawInt(buffer, ref index, count);
            for (int i = 0; i < count; i++) buffer[index++] = val[i];
        }

        public static void Write<T>(byte[] buffer, ref int index, ushort tag, List<T> val) where T : IBinaryData
        {
            WriteHeader(buffer, ref index, tag, WireType.String);
            if (val == null) { WriteRawInt(buffer, ref index, 0); return; }

            // 占位 TotalLen
            int lenPos = index; 
            index += 4;
            int startDataPos = index;

            WriteRawInt(buffer, ref index, val.Count);
            
            foreach (var item in val)
            {
                if (item == null) 
                {
                    WriteRawInt(buffer, ref index, 0);
                }
                else
                {
                    WriteRawInt(buffer, ref index, item.Length());
                    item.Writing(buffer, ref index);
                }
            }

            // 回填 TotalLen
            int endPos = index;
            int totalLen = endPos - startDataPos;
            int tempIdx = lenPos;
            WriteRawInt(buffer, ref tempIdx, totalLen);
        }

        // Struct 用于 Float/Int 转换，避免 BitConverter 分配内存
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct UnionIntFloat
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public int IntValue;
            [System.Runtime.InteropServices.FieldOffset(0)] public float FloatValue;
        }
    }
}