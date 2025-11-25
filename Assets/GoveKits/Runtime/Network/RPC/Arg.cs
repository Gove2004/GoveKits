using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GoveKits.Save;



namespace GoveKits.Network
{
    // 支持的参数类型枚举
    public enum RpcArgType : byte
    {
        Int = 1,
        Float = 2,
        Bool = 3,
        String = 4,
        Vector3 = 5,
        Long = 6
    }

    /// <summary>
    /// 扩展 BinaryData 的能力，使其能读写 object
    /// </summary>
    public static class ArgExtensions
    {
        // 写入动态参数：先写类型(byte)，再写数据
        public static void WriteArg(byte[] buffer, object data, ref int index)
        {
            if (data is int i) {
                buffer[index++] = (byte)RpcArgType.Int;
                BinaryData.WriteInt(buffer, i, ref index);
            }
            else if (data is float f) {
                buffer[index++] = (byte)RpcArgType.Float;
                BinaryData.WriteFloat(buffer, f, ref index);
            }
            else if (data is bool b) {
                buffer[index++] = (byte)RpcArgType.Bool;
                BinaryData.WriteBool(buffer, b, ref index);
            }
            else if (data is string s) {
                buffer[index++] = (byte)RpcArgType.String;
                BinaryData.WriteString(buffer, s, ref index);
            }
            else if (data is Vector3 v) {
                buffer[index++] = (byte)RpcArgType.Vector3;
                BinaryData.WriteVector3(buffer, v, ref index);
            }
            // ... 可自行扩展 Long, Double 等
            else {
                Debug.LogError($"[RPC] Unsupported type: {data.GetType()}");
            }
        }

        // 读取动态参数：先读类型，再根据类型读数据
        public static object ReadArg(byte[] buffer, ref int index)
        {
            RpcArgType type = (RpcArgType)buffer[index++];
            switch (type)
            {
                case RpcArgType.Int: return BinaryData.ReadInt(buffer, ref index);
                case RpcArgType.Float: return BinaryData.ReadFloat(buffer, ref index);
                case RpcArgType.Bool: return BinaryData.ReadBool(buffer, ref index);
                case RpcArgType.String: return BinaryData.ReadString(buffer, ref index);
                case RpcArgType.Vector3: return BinaryData.ReadVector3(buffer, ref index);
                default: return null;
            }
        }

        // 计算动态参数长度
        public static int GetArgLength(object data)
        {
            int typeHeader = 1; // 1 byte for Type
            if (data is int) return typeHeader + 4;
            if (data is float) return typeHeader + 4;
            if (data is bool) return typeHeader + 1;
            if (data is Vector3) return typeHeader + 12;
            if (data is string s) return typeHeader + 4 + Encoding.UTF8.GetByteCount(s);
            return 0;
        }
    }
}