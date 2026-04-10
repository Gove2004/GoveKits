using System;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 存档序列化器。
    /// </summary>
    public interface ISerializer
    {
        string FileExtension { get; }
        /// <summary>
        /// 将对象序列化为二进制数据。
        /// </summary>
        byte[] Serialize(object data, Type dataType);

        /// <summary>
        /// 将二进制数据反序列化为对象。
        /// </summary>
        object Deserialize(byte[] bytes, Type dataType);
    }
}