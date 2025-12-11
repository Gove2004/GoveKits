using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GoveKits.Binary
{
    public static class BinaryLengthHelper
    {
        // 基础开销: Tag(2) + Wire(1) = 3
        private const int HeaderSize = 3;
        private const int LenIntSize = 4; // 长度前缀占用4字节

        // --- Primitives ---
        public static int Get(int v) => HeaderSize + 4;
        public static int Get(float v) => HeaderSize + 4;
        public static int Get(bool v) => HeaderSize + 1;
        public static int Get(long v) => HeaderSize + 8;
        
        // --- Unity Types ---
        public static int Get(Vector2 v) => HeaderSize + 8;
        public static int Get(Vector3 v) => HeaderSize + 12;
        public static int Get(Quaternion v) => HeaderSize + 16;

        // --- String (Var Length) ---
        // 格式: Tag(3) + BodyLen(4) + UTF8Bytes
        public static int Get(string v) 
            => HeaderSize + LenIntSize + (string.IsNullOrEmpty(v) ? 0 : Encoding.UTF8.GetByteCount(v));

        // --- Nested Object ---
        // 格式: Tag(3) + BodyLen(4) + ObjectBytes
        public static int Get<T>(T v) where T : IBinaryData 
            => HeaderSize + LenIntSize + (v == null ? 0 : v.Length());

        // --- Lists ---
        // 格式: Tag(3) + TotalBodyLen(4) + Count(4) + Items
        
        public static int Get(List<byte> v) 
            => HeaderSize + LenIntSize + 4 + (v == null ? 0 : v.Count);

        public static int Get(List<bool> v) 
            => HeaderSize + LenIntSize + 4 + (v == null ? 0 : v.Count); // bool 按 1 byte 存

        public static int Get(List<int> v) 
            => HeaderSize + LenIntSize + 4 + (v == null ? 0 : v.Count * 4);

        public static int Get(List<float> v) 
            => HeaderSize + LenIntSize + 4 + (v == null ? 0 : v.Count * 4);
        
        public static int Get(List<string> v)
        {
            // TotalBodyLen(4) + Count(4)
            int bodyLen = 4 + 4; 
            if (v != null)
            {
                foreach (var s in v)
                    // 每个 String: Len(4) + Content
                    bodyLen += 4 + (string.IsNullOrEmpty(s) ? 0 : Encoding.UTF8.GetByteCount(s));
            }
            return HeaderSize + bodyLen;
        }

        public static int Get<T>(List<T> v) where T : IBinaryData
        {
            int bodyLen = 4 + 4;
            if (v != null)
            {
                foreach (var item in v)
                    // 每个 Object: Len(4) + Content (如果是null则Len=0)
                    bodyLen += 4 + (item == null ? 0 : item.Length());
            }
            return HeaderSize + bodyLen;
        }
    }
}