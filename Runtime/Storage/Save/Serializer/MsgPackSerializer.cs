using System;
using MessagePack;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 基于 MessagePack-CSharp 的二进制序列化器。适用于对性能和体积有要求的存档场景。
    /// </summary>
    public sealed class MsgPackSerializer : ISerializer
    {
        /// <summary>
        /// 生成的文件扩展名。
        /// </summary>
        public string FileExtension => ".msgpack";

        /// <summary>
        /// 将对象序列化为 MessagePack 字节数组。
        /// </summary>
        /// <param name="data">要序列化的数据对象。</param>
        /// <param name="dataType">数据的类型。</param>
        /// <returns>MessagePack 格式的字节数组。</returns>
        public byte[] Serialize(object data, Type dataType)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            return MessagePackSerializer.Serialize(dataType, data);
        }

        /// <summary>
        /// 将 MessagePack 字节数组反序列化为指定类型的对象。
        /// </summary>
        /// <param name="bytes">要反序列化的字节数组。</param>
        /// <param name="dataType">目标类型。</param>
        /// <returns>反序列化后的对象。</returns>
        public object Deserialize(byte[] bytes, Type dataType)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            return MessagePackSerializer.Deserialize(dataType, bytes);
        }
    }
}
