using System;
using System.Text;


namespace GoveKits.Save
{
    /// <summary>
    /// 二进制数据基类，提供读写方法
    /// </summary>
    public abstract class BinaryData
    {
        /// <summary>
        /// 获取数据的二进制长度
        /// </summary>
        public abstract int Length();

        /// <summary>
        /// 将数据写入目标 Buffer (核心修改：不返回数组，直接写入)
        /// </summary>
        public abstract void Writing(byte[] buffer, ref int index);

        /// <summary>
        /// 从字节数组中读取数据
        /// </summary>
        public abstract void Reading(byte[] buffer, ref int index);

        // ========== 辅助方法 ==========

        protected static void EnsureAvailable(byte[] bytes, int index, int length)
        {
            if (index + length > bytes.Length) 
                throw new ArgumentOutOfRangeException($"Buffer overflow: index={index}, req={length}, size={bytes.Length}");
        }

        // ========== 0 GC 写方法 (使用位移) ==========

        public void WriteBool(byte[] bytes, bool value, ref int index)
        {
            EnsureAvailable(bytes, index, 1);
            bytes[index++] = value ? (byte)1 : (byte)0;
        }

        public void WriteByte(byte[] bytes, byte value, ref int index)
        {
            EnsureAvailable(bytes, index, 1);
            bytes[index++] = value;
        }

        // ========== 0 GC 写方法 (改为 Little-Endian 小端序) ==========
        // 顺序：低位在前，高位在后

        public void WriteShort(byte[] bytes, short value, ref int index)
        {
            EnsureAvailable(bytes, index, 2);
            bytes[index++] = (byte)value;          // 低位
            bytes[index++] = (byte)(value >> 8);   // 高位
        }

        public void WriteInt(byte[] bytes, int value, ref int index)
        {
            EnsureAvailable(bytes, index, 4);
            bytes[index++] = (byte)value;          // 低8位
            bytes[index++] = (byte)(value >> 8);
            bytes[index++] = (byte)(value >> 16);
            bytes[index++] = (byte)(value >> 24);  // 高8位
        }

        public void WriteLong(byte[] bytes, long value, ref int index)
        {
            EnsureAvailable(bytes, index, 8);
            for (int i = 0; i < 8; i++) 
            {
                bytes[index++] = (byte)(value >> (i * 8));
            }
        }

        public void WriteFloat(byte[] bytes, float value, ref int index)
        {
            // Unity/C# 的 BitConverter 默认就是小端，不需要反转，但会有GC
            // 使用指针或转换来避免GC
            int intVal = BitConverter.SingleToInt32Bits(value);
            WriteInt(bytes, intVal, ref index);
        }

        public void WriteDouble(byte[] bytes, double value, ref int index)
        {
            long longVal = BitConverter.DoubleToInt64Bits(value);
            WriteLong(bytes, longVal, ref index);
        }

        public void WriteVector2(byte[] bytes, UnityEngine.Vector2 value, ref int index)
        {
            WriteFloat(bytes, value.x, ref index);
            WriteFloat(bytes, value.y, ref index);
        }

        public void WriteVector3(byte[] bytes, UnityEngine.Vector3 value, ref int index)
        {
            WriteFloat(bytes, value.x, ref index);
            WriteFloat(bytes, value.y, ref index);
            WriteFloat(bytes, value.z, ref index);
        }
        
        public void WriteString(byte[] bytes, string value, ref int index)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteInt(bytes, 0, ref index);
                return;
            }
            // 计算字节长度
            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteInt(bytes, byteCount, ref index);
            EnsureAvailable(bytes, index, byteCount);
            // 直接写入，避免 GetBytes() 产生临时数组
            Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, index);
            index += byteCount;
        }

        // 嵌套数据写入优化：直接透传 buffer
        public void WriteData(byte[] bytes, BinaryData dataValue, ref int index)
        {
            // 不再需要 dataValue.Writing() 创建中间数组
            // 也不需要 EnsureAvailable，因为 dataValue 内部会自己检查
            dataValue.Writing(bytes, ref index); 
        }

        // ========== 读方法 (位移) ==========

        public bool ReadBool(byte[] bytes, ref int index)
        {
            EnsureAvailable(bytes, index, 1);
            return bytes[index++] != 0;
        }

        public byte ReadByte(byte[] bytes, ref int index)
        {
            EnsureAvailable(bytes, index, 1);
            return bytes[index++];
        }

        // ========== 读方法 (改为 Little-Endian 小端序) ==========

        public short ReadShort(byte[] bytes, ref int index)
        {
            EnsureAvailable(bytes, index, 2);
            short val = (short)(bytes[index] | (bytes[index + 1] << 8));
            index += 2;
            return val;
        }

        public int ReadInt(byte[] bytes, ref int index)
        {
            EnsureAvailable(bytes, index, 4);
            int val = bytes[index] | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24);
            index += 4;
            return val;
        }

        public float ReadFloat(byte[] bytes, ref int index)
        {
            int intVal = ReadInt(bytes, ref index);
            return BitConverter.Int32BitsToSingle(intVal);
        }

        public long ReadLong(byte[] bytes, ref int index)
        {
            EnsureAvailable(bytes, index, 8);
            long val = 0;
            for (int i = 0; i < 8; i++)
            {
                val |= ((long)bytes[index + i] << (i * 8));
            }
            index += 8;
            return val;
        }

        public double ReadDouble(byte[] bytes, ref int index)
        {
            long longVal = ReadLong(bytes, ref index);
            return BitConverter.Int64BitsToDouble(longVal);
        }

        public UnityEngine.Vector2 ReadVector2(byte[] bytes, ref int index)
        {
            float x = ReadFloat(bytes, ref index);
            float y = ReadFloat(bytes, ref index);
            return new UnityEngine.Vector2(x, y);
        }

        public UnityEngine.Vector3 ReadVector3(byte[] bytes, ref int index)
        {
            float x = ReadFloat(bytes, ref index);
            float y = ReadFloat(bytes, ref index);
            float z = ReadFloat(bytes, ref index);
            return new UnityEngine.Vector3(x, y, z);
        }

        public string ReadString(byte[] bytes, ref int index)
        {
            int len = ReadInt(bytes, ref index);
            EnsureAvailable(bytes, index, len);
            string s = Encoding.UTF8.GetString(bytes, index, len);
            index += len;
            return s;
        }

        public T ReadData<T>(byte[] bytes, ref int index) where T : BinaryData, new()
        {
            T data = new T();
            // 移除 data.Length() 检查，因为空对象的 Length 不代表实际数据 Length
            data.Reading(bytes, ref index);
            return data;
        }
    }
}