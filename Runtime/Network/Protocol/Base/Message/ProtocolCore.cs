using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoveKits.Runtime.Core;
using MessagePack;
using MessagePack.Resolvers;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 协议注册与序列化器（实例化版本）
    /// </summary>
    public static class ProtocolCore
    {
        private static readonly Dictionary<ushort, Type> _idToType = new();
        private static readonly Dictionary<Type, ushort> _typeToId = new();
        private static MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard;

        public static void ScanAndRegister()
        {
            _idToType.Clear();
            _typeToId.Clear();

            var msgTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IProtocolMessage).IsAssignableFrom(t) 
                         && t.GetCustomAttribute<ProtocolIdAttribute>() != null);

            foreach (var type in msgTypes)
            {
                var attr = type.GetCustomAttribute<ProtocolIdAttribute>();
                if (_idToType.ContainsKey(attr.Id))
                    throw new InvalidOperationException($"Protocol ID Conflict: {attr.Id} on {type.Name}");

                _idToType[attr.Id] = type;
                _typeToId[type] = attr.Id;
            }

            LogCore.Success(nameof(ProtocolCore), $"协议注册完成，Total Protocols: {_idToType.Count}");
        }

        public static void AddResolver(IFormatterResolver resolver)
        {
            _options = _options.WithResolver(CompositeResolver.Create(resolver, _options.Resolver));
        }

        public static ushort GetId(Type type) => _typeToId.GetValueOrDefault(type, (ushort)0);
        public static ushort GetId<T>() => _typeToId.GetValueOrDefault(typeof(T), (ushort)0);
        public static Type GetType(ushort id) => _idToType.GetValueOrDefault(id);

        public static byte[] Serialize<T>(T message) where T : IProtocolMessage
        {
            return MessagePackSerializer.Serialize(message, _options);
        }

        public static IProtocolMessage Deserialize(ushort id, ReadOnlyMemory<byte> purePayload)
        {
            Type type = GetType(id);
            if (type == null) return null;
            return (IProtocolMessage)MessagePackSerializer.Deserialize(type, purePayload, _options);
        }


        // 供外部使用的通用版本
        public static byte[] Serialize(Type type, object obj)
        {
            return MessagePackSerializer.Serialize(type, obj, _options);
        }

        public static object Deserialize(Type type, byte[] purePayload)
        {
            if (purePayload == null || purePayload.Length == 0) return null;
            return MessagePackSerializer.Deserialize(type, purePayload, _options);
        }
    }
}