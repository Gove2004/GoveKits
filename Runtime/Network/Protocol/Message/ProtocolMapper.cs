using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoveKits.Runtime.Network
{
    public interface IProtocolMessage { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ProtocolIdAttribute : Attribute
    {
        public ushort Id { get; }
        public ProtocolIdAttribute(ushort id) => Id = id;
    }
    

    internal class ProtocolMapper
    {
        private readonly Dictionary<ushort, Type> _idToType = new();
        private readonly Dictionary<Type, ushort> _typeToId = new();

        public void ScanAndRegister()
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
                    throw new InvalidOperationException($"Protocol ID Conflict: {attr.Id}");

                _idToType[attr.Id] = type;
                _typeToId[type] = attr.Id;
            }
        }

        public ushort GetId(Type type) => _typeToId.GetValueOrDefault(type, (ushort)0);
        public ushort GetId<T>() => _typeToId.GetValueOrDefault(typeof(T), (ushort)0);
        public Type GetType(ushort id) => _idToType.GetValueOrDefault(id);
    }
}