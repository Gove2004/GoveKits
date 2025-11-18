using System;

// 二进制数据接口
// Length 获取数据的二进制长度
// Writing 将数据写入字节数组并返回字节数组
// Reading 从字节数组中读取数据，返回读取的字节数

public abstract class BinaryData
{
    /// <summary>
    /// 获取数据的二进制长度
    /// </summary>
    public abstract int Length();
    /// <summary>
    /// 从字节数组中读取数据，返回读取的字节数
    /// </summary>
    public abstract int Reading(byte[] data, int index);
    /// <summary>
    /// 将数据写入字节数组并返回字节数组
    /// </summary>
    public abstract byte[] Writing();



    // ========== 辅助方法 ==========

    private static void EnsureAvailable(byte[] bytes, int index, int length)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (index + length > bytes.Length) throw new ArgumentOutOfRangeException(nameof(bytes), $"Not enough bytes in buffer (index={index}, length={length}, bufferLength={bytes.Length})");
    }

    // ========== 写方法 ==========
    
    public void WriteBool(byte[] bytes, bool value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(bool));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(bool);
    }

    public void WriteByte(byte[] bytes, byte value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(byte));
        bytes[index] = value;
        index += sizeof(byte);
    }

    public void WriteChar(byte[] bytes, char value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(char));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(char);
    }

    public void WriteShort(byte[] bytes, short value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(short));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(short);
    }

    public void WriteInt(byte[] bytes, int value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(int));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(int);
    }

    public void WriteLong(byte[] bytes, long value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(long));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(long);
    }

    public void WriteFloat(byte[] bytes, float value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(float));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(float);
    }

    public void WriteDouble(byte[] bytes, double value, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(double));
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(double);
    }

    public void WriteString(byte[] bytes, string value, ref int index)
    {
        byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteInt(bytes, stringBytes.Length, ref index);
        EnsureAvailable(bytes, index, stringBytes.Length);
        Array.Copy(stringBytes, 0, bytes, index, stringBytes.Length);
        index += stringBytes.Length;
    }

    public void WriteByteArray(byte[] bytes, byte[] value, ref int index)
    {
        WriteInt(bytes, value.Length, ref index);
        EnsureAvailable(bytes, index, value.Length);
        Array.Copy(value, 0, bytes, index, value.Length);
        index += value.Length;
    }

    public void WriteData(byte[] bytes, BinaryData dataValue, ref int index)
    {
        byte[] dataBytes = dataValue.Writing();
        // WriteInt(bytes, dataBytes.Length, ref index);  // 注释掉：不再写入数据长度
        EnsureAvailable(bytes, index, dataBytes.Length);
        Array.Copy(dataBytes, 0, bytes, index, dataBytes.Length);
        index += dataBytes.Length;
    }

    // ========== 读方法 ==========
    
    public bool ReadBool(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(bool));
        bool value = BitConverter.ToBoolean(bytes, index);
        index += sizeof(bool);
        return value;
    }

    public byte ReadByte(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(byte));
        byte value = bytes[index];
        index += sizeof(byte);
        return value;
    }

    public char ReadChar(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(char));
        char value = BitConverter.ToChar(bytes, index);
        index += sizeof(char);
        return value;
    }

    public short ReadShort(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(short));
        short value = BitConverter.ToInt16(bytes, index);
        index += sizeof(short);
        return value;
    }

    public int ReadInt(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(int));
        int value = BitConverter.ToInt32(bytes, index);
        index += sizeof(int);
        return value;
    }

    public long ReadLong(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(long));
        long value = BitConverter.ToInt64(bytes, index);
        index += sizeof(long);
        return value;
    }

    public float ReadFloat(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(float));
        float value = BitConverter.ToSingle(bytes, index);
        index += sizeof(float);
        return value;
    }

    public double ReadDouble(byte[] bytes, ref int index)
    {
        EnsureAvailable(bytes, index, sizeof(double));
        double value = BitConverter.ToDouble(bytes, index);
        index += sizeof(double);
        return value;
    }

    public string ReadString(byte[] bytes, ref int index)
    {
        int length = ReadInt(bytes, ref index);
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Negative string length");
        EnsureAvailable(bytes, index, length);
        string value = System.Text.Encoding.UTF8.GetString(bytes, index, length);
        index += length;
        return value;
    }

    public byte[] ReadByteArray(byte[] bytes, ref int index)
    {
        int length = ReadInt(bytes, ref index);
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Negative array length");
        EnsureAvailable(bytes, index, length);
        byte[] value = new byte[length];
        Array.Copy(bytes, index, value, 0, length);
        index += length;
        return value;
    }

    public T ReadData<T>(byte[] bytes, ref int index) where T : BinaryData, new()
    {
        // int length = ReadInt(bytes, ref index);  // 注释掉：不再读取数据长度
        T data = new T();
        int length = data.Length();
        EnsureAvailable(bytes, index, length);
        byte[] dataBytes = new byte[length];
        Array.Copy(bytes, index, dataBytes, 0, length);
        index += length;
        data.Reading(dataBytes, 0);
        return data;
    }
}