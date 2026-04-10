using System;
using System.Reflection;
using Google.Protobuf;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 基于 Google.Protobuf 的序列化器。
    /// </summary>
    public sealed class ProtobufSerializer : ISerializer
    {
        public string FileExtension => ".proto";
        
        public byte[] Serialize(object data, Type dataType)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));
            if (data is not IMessage message)
            {
                throw new ArgumentException($"Save data type '{dataType.FullName}' must implement IMessage.");
            }

            return message.ToByteArray();
        }

        public object Deserialize(byte[] bytes, Type dataType)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));
            if (!typeof(IMessage).IsAssignableFrom(dataType))
            {
                throw new ArgumentException($"Save data type '{dataType.FullName}' must implement IMessage.");
            }

            PropertyInfo parserProperty = dataType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
            if (parserProperty == null)
            {
                throw new InvalidOperationException($"Type '{dataType.FullName}' does not expose static Parser property.");
            }

            if (parserProperty.GetValue(null) is not MessageParser parser)
            {
                throw new InvalidOperationException($"Type '{dataType.FullName}' Parser is not a MessageParser.");
            }

            return parser.ParseFrom(bytes);
        }
    }
}