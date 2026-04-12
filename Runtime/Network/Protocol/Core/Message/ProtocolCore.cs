// Protocol/ProtocolCore.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoveKits.Runtime.Core;
using MessagePack;
using MessagePack.Resolvers;

namespace GoveKits.Runtime.Network
{
    public static class ProtocolCore
    {
        private static Dictionary<ushort, Type> _idToType = new();
        private static Dictionary<Type, ushort> _typeToId = new();
        
        // MessagePack 配置
        private static MessagePackSerializerOptions _options;

        /// <summary>
        /// 添加 Resolver（可多次调用）
        /// </summary>
        public static void AddResolver(IFormatterResolver resolver)
        {
            // 延迟初始化 options
            if (_options == null)
            {
                _options = MessagePackSerializerOptions.Standard;
            }
            
            // 组合新的 Resolver
            var currentResolver = _options.Resolver;
            var combined = CompositeResolver.Create(resolver, currentResolver);
            _options = _options.WithResolver(combined);
        }

        /// <summary>
        /// 扫描所有协议类型
        /// </summary>
        public static void ScanProtocols()
        {
            Clear();

            var msgTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IProtocolMessage).IsAssignableFrom(t) 
                           && t.GetCustomAttribute<ProtocolIdAttribute>() != null);

            foreach (var type in msgTypes)
            {
                var attr = type.GetCustomAttribute<ProtocolIdAttribute>();
                if (_idToType.ContainsKey(attr.Id))
                    throw new InvalidOperationException($"消息ID冲突: {attr.Id}");

                _idToType[attr.Id] = type;
                _typeToId[type] = attr.Id;
            }

            LogCore.Success(nameof(ProtocolCore), $"自动注册 {_idToType.Count} 个消息类型");
        }

        // --- 路由查询 ---
        public static ushort GetId<T>() where T : IProtocolMessage => 
            _typeToId.GetValueOrDefault(typeof(T), (ushort)0);
        
        public static ushort GetId(Type type) => 
            _typeToId.GetValueOrDefault(type, (ushort)0);
        
        public static Type GetType(ushort id) => 
            _idToType.GetValueOrDefault(id);

        // --- 序列化/反序列化 ---
        public static byte[] Serialize<T>(T message) where T : IProtocolMessage
        {
            return MessagePackSerializer.Serialize(message, _options);
        }

        public static T Deserialize<T>(byte[] data) where T : IProtocolMessage
        {
            return MessagePackSerializer.Deserialize<T>(data, _options);
        }

        public static IProtocolMessage Deserialize(ushort protocolId, byte[] data)
        {
            var type = GetType(protocolId);
            if (type == null) return null;
            return (IProtocolMessage)MessagePackSerializer.Deserialize(type, data, _options);
        }

        public static void Clear()
        {
            _idToType.Clear();
            _typeToId.Clear();
            _options = null;
        }
    }
}