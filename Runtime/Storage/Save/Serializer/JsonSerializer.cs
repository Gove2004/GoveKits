using System;
using System.Text;
using Newtonsoft.Json;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 基于 Newtonsoft.Json 的 JSON 序列化器，用于存档数据的持久化。
    /// </summary>
    public sealed class JsonSerializer : ISerializer
    {
        /// <summary>
        /// 生成的文件扩展名。
        /// </summary>
        public string FileExtension => ".json";

        /// <summary>
        /// 将对象序列化为 UTF-8 编码的 JSON 字节数组。
        /// </summary>
        /// <param name="data">要序列化的数据对象。</param>
        /// <param name="dataType">数据的类型。</param>
        /// <returns>JSON 格式的字节数组。</returns>
        public byte[] Serialize(object data, Type dataType)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            string json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 将 UTF-8 字节数组反序列化为指定类型的对象。
        /// </summary>
        /// <param name="bytes">要反序列化的字节数组。</param>
        /// <param name="dataType">目标类型。</param>
        /// <returns>反序列化后的对象。</returns>
        public object Deserialize(byte[] bytes, Type dataType)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject(json, dataType);
        }
    }
}
