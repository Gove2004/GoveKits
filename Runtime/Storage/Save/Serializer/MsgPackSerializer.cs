using MessagePack;
using System;

namespace GoveKits.Runtime.Storage
{
    public class MsgPackSerializer : ISerializer
    {
        public string FileExtension => ".msgpack";

        public byte[] Serialize(object data, Type dataType)
        {
            return MessagePackSerializer.Serialize(dataType, data);
        }

        public object Deserialize(byte[] bytes, Type dataType)
        {
            return MessagePackSerializer.Deserialize(dataType, bytes);
        }
    }
}