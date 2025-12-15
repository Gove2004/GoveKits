using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GoveKits.Binary
{
    public static class BinaryReadHelper
    {
        // --- Header & Skip ---
        public static bool ReadHeader(byte[] buffer, ref int index, int endPos, out ushort tag, out WireType type)
        {
            // 确保有足够的字节读 Header (3 bytes)
            if (index + 3 > endPos) 
            { 
                tag = 0; type = 0; 
                return false; 
            }
            tag = (ushort)(buffer[index++] | (buffer[index++] << 8));
            type = (WireType)buffer[index++];
            return true;
        }

        public static void Skip(byte[] buffer, ref int index, WireType type)
        {
            switch (type) {
                case WireType.Fixed1: index += 1; break;
                case WireType.Fixed4: index += 4; break;
                case WireType.Fixed8: index += 8; break;
                case WireType.Fixed12: index += 12; break;
                case WireType.Fixed16: index += 16; break;
                case WireType.String:
                    int len = ReadRawInt(buffer, ref index);
                    index += len;
                    break;
            }
        }

        private static int ReadRawInt(byte[] buffer, ref int index) 
            => buffer[index++] | (buffer[index++] << 8) | (buffer[index++] << 16) | (buffer[index++] << 24);

        // --- Primitives ---
        public static void Read(byte[] buffer, ref int index, out int val) => val = ReadRawInt(buffer, ref index);
        
        public static void Read(byte[] buffer, ref int index, out float val)
        {
            int i = ReadRawInt(buffer, ref index);
            UnionIntFloat u = new UnionIntFloat { IntValue = i };
            val = u.FloatValue;
        }
        
        public static void Read(byte[] buffer, ref int index, out bool val) => val = buffer[index++] != 0;

        public static void Read(byte[] buffer, ref int index, out long val)
        {
            int low = ReadRawInt(buffer, ref index);
            int high = ReadRawInt(buffer, ref index);
            val = ((long)high << 32) | (uint)low;
        }

        // --- Unity Types ---
        public static void Read(byte[] buffer, ref int index, out Vector3 val)
        {
            Read(buffer, ref index, out float x);
            Read(buffer, ref index, out float y);
            Read(buffer, ref index, out float z);
            val = new Vector3(x, y, z);
        }

        public static void Read(byte[] buffer, ref int index, out Quaternion val)
        {
            Read(buffer, ref index, out float x);
            Read(buffer, ref index, out float y);
            Read(buffer, ref index, out float z);
            Read(buffer, ref index, out float w);
            val = new Quaternion(x, y, z, w);
        }

        // --- String ---
        public static void Read(byte[] buffer, ref int index, out string val)
        {
            int len = ReadRawInt(buffer, ref index);
            if (len == 0) 
            {
                val = string.Empty;
            }
            else
            {
                val = Encoding.UTF8.GetString(buffer, index, len);
                index += len;
            }
        }

        // --- Nested Object ---
        public static void Read<T>(byte[] buffer, ref int index, out T val) where T : IBinaryData, new()
        {
            int len = ReadRawInt(buffer, ref index); // 读取 BodyLength
            if (len == 0) 
            {
                val = default; 
                return; 
            }

            int endObjPos = index + len;
            val = new T();
            // 传入 endPos 以便内部处理版本兼容
            val.Reading(buffer, ref index, endObjPos);

            // 安全校验：如果 Reading 没读完（或者读多了），强制修正到 endPos
            if (index != endObjPos)
            {
                LogManager.LogWarning("BinaryReadHelper", $"Data mismatch for {typeof(T)}. Expected end: {endObjPos}, Actual: {index}");
                index = endObjPos;
            }
        }

        // --- Lists ---
        public static void Read(byte[] buffer, ref int index, out List<int> val)
        {
            int totalLen = ReadRawInt(buffer, ref index);
            if (totalLen == 0) { val = null; return; }

            int endListPos = index + totalLen;
            int count = ReadRawInt(buffer, ref index);
            
            val = new List<int>(count);
            for(int i=0; i<count; i++) val.Add(ReadRawInt(buffer, ref index));

            if (index != endListPos) index = endListPos;
        }

        public static void Read(byte[] buffer, ref int index, out List<byte> val)
        {
            int totalLen = ReadRawInt(buffer, ref index);
            if (totalLen == 0) { val = null; return; }

            int endListPos = index + totalLen;
            int count = ReadRawInt(buffer, ref index);
            
            val = new List<byte>(count);
            for(int i=0; i<count; i++) val.Add(buffer[index++]);

            if (index != endListPos) index = endListPos;
        }

        public static void Read<T>(byte[] buffer, ref int index, out List<T> val) where T : IBinaryData, new()
        {
            int totalLen = ReadRawInt(buffer, ref index);
            if (totalLen == 0) { val = null; return; }

            int endListPos = index + totalLen;
            int count = ReadRawInt(buffer, ref index);
            
            val = new List<T>(count);
            for(int i=0; i<count; i++)
            {
                // 读取单个对象 (Length + Body)
                int itemLen = ReadRawInt(buffer, ref index);
                if (itemLen == 0) 
                {
                    val.Add(default); // null item
                }
                else
                {
                    T item = new T();
                    int itemEndPos = index + itemLen;
                    item.Reading(buffer, ref index, itemEndPos);
                    if (index != itemEndPos) index = itemEndPos; // 单项修正
                    val.Add(item);
                }
            }

            if (index != endListPos) index = endListPos; // 列表整体修正
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct UnionIntFloat
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public int IntValue;
            [System.Runtime.InteropServices.FieldOffset(0)] public float FloatValue;
        }
    }
}