// Protocol/ProtocolCore.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public class ProtocolCore : ICore
    {
        private readonly Dictionary<ushort, Type> _idToType = new();
        private readonly Dictionary<Type, ushort> _typeToId = new();
        
        // 序列化器由 ProtocolCore 独占持有
        private readonly IProtocolMessageSerializer _serializer;

        // 构造时注入序列化器
        public ProtocolCore(IProtocolMessageSerializer serializer)
        {
            _serializer = serializer ?? new StandardMessagePackSerializer();

            // 自动注册协议
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
            
            CoreLocator.Log.Success(nameof(ProtocolCore), $"自动注册 {_idToType.Count} 个消息类型");
        }

        // --- 路由查询 ---
        public ushort GetId<T>() where T : IProtocolMessage => _typeToId.GetValueOrDefault(typeof(T), (ushort)0);
        public ushort GetId(Type type) => _typeToId.GetValueOrDefault(type, (ushort)0);
        public Type GetType(ushort id) => _idToType.GetValueOrDefault(id);

        // --- 序列化/反序列化入口 ---
        public byte[] Serialize<T>(T message) where T : IProtocolMessage
        {
            return _serializer.Serialize(message);
        }

        public IProtocolMessage Deserialize(ushort protocolId, byte[] payload)
        {
            var type = GetType(protocolId);
            if (type == null) return null;
            return _serializer.Deserialize(type, payload);
        }

        public void OnShutdown()
        {
            _idToType.Clear();
            _typeToId.Clear();
        }
    }
}