// Protocol/ProtocolRegistry.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoveKits.Runtime.Network
{
    public static class ProtocolRegistry
    {
        private static readonly Dictionary<ushort, Type> _idToType = new();
        private static readonly Dictionary<Type, ushort> _typeToId = new();
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            var msgTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IProtocolMessage).IsAssignableFrom(t) 
                           && t.GetCustomAttribute<ProtocolIdAttribute>() != null);

            foreach (var type in msgTypes)
            {
                var attr = type.GetCustomAttribute<ProtocolIdAttribute>();
                if (_idToType.ContainsKey(attr.Id))
                    throw new InvalidOperationException($"消息ID冲突: {attr.Id} 已被 {_idToType[attr.Id].Name} 使用");

                _idToType[attr.Id] = type;
                _typeToId[type] = attr.Id;
            }

            _initialized = true;
            UnityEngine.Debug.Log($"[ProtocolRegistry] 注册 {_idToType.Count} 个消息类型");
        }

        public static ushort GetId<T>() where T : IProtocolMessage
            => _typeToId.GetValueOrDefault(typeof(T), (ushort)0);

        public static ushort GetId(Type type)
            => _typeToId.GetValueOrDefault(type, (ushort)0);

        public static Type GetType(ushort id)
            => _idToType.GetValueOrDefault(id);

        public static bool IsRegistered(ushort id) => _idToType.ContainsKey(id);
    }
}