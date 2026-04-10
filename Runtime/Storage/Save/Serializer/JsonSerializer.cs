using System;
using System.Text;
using Newtonsoft.Json;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 基于 Newtonsoft.Json 的序列化器。
    /// </summary>
    public sealed class JsonSerializer : ISerializer
    {
        public string FileExtension => ".json";

        public byte[] Serialize(object data, Type dataType)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            string json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }

        public object Deserialize(byte[] bytes, Type dataType)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject(json, dataType);
        }
    }
}