using System;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 数据序列化接口。实现此接口以支持不同的存档序列化格式（JSON 等）。
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 生成的文件扩展名（含点）。例如: ".json"。
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// 将对象序列化为字节数组。
        /// </summary>
        /// <param name="data">要序列化的数据对象。</param>
        /// <param name="dataType">数据的类型。</param>
        /// <returns>序列化后的字节数组。</returns>
        byte[] Serialize(object data, Type dataType);

        /// <summary>
        /// 从字节数组反序列化为对象。
        /// </summary>
        /// <param name="bytes">要反序列化的字节数组。</param>
        /// <param name="dataType">目标类型。</param>
        /// <returns>反序列化后的对象。</returns>
        object Deserialize(byte[] bytes, Type dataType);
    }
}
