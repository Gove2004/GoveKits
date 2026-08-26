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
    /// 协议注册与 MessagePack 序列化核心。
    /// 负责扫描标注 [ProtocolId] 的消息类型、维护协议 ID 与类型的双向映射、
    /// 以及执行 MessagePack 格式的序列化和反序列化操作。
    /// </summary>
    public sealed class ProtocolCenter
    {
        private readonly Dictionary<ushort, Type> _idToType = new();
        private readonly Dictionary<Type, ushort> _typeToId = new();
        private MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard;

        /// <summary>
        /// 扫描当前 AppDomain 中所有程序集，查找标注了 [ProtocolId] 的特性类型并注册到映射表中。
        /// 重复调用会先清空已有注册。遇到动态程序集或无法加载的类型时会跳过并记录警告。
        /// </summary>
        public void ScanAndRegister()
        {
            _idToType.Clear();
            _typeToId.Clear();

            var msgTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                try
                {
                    var types = assembly.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (types[i] != null) msgTypes.Add(types[i]);
                    }
                }
                catch (Exception ex)
                {
                    LogCore.Warning(nameof(ProtocolCenter), $"扫描程序集失败 {assembly.FullName}: {ex.Message}");
                }
            }

            foreach (var type in msgTypes)
            {
                var attr = type.GetCustomAttribute<ProtocolIdAttribute>();
                if (attr != null)
                {
                    if (_idToType.ContainsKey(attr.Id))
                        throw new InvalidOperationException($"协议 ID 冲突: {attr.Id} 已分配给 {type.Name}");

                    _idToType[attr.Id] = type;
                    _typeToId[type] = attr.Id;
                }
            }

            LogCore.Success(nameof(ProtocolCenter), $"已注册 {_idToType.Count} 个协议消息。");
        }

        /// <summary>
        /// 添加自定义的 MessagePack 格式化器解析器，使其参与序列化和反序列化过程。
        /// 新解析器会通过 CompositeResolver 与已有解析器链组合。
        /// </summary>
        /// <param name="resolver">要注册的格式化器解析器。</param>
        public void AddResolver(IFormatterResolver resolver)
        {
            _options = _options.WithResolver(CompositeResolver.Create(resolver, _options.Resolver));
        }

        /// <summary>
        /// 根据协议消息类型获取其注册的协议 ID。
        /// </summary>
        /// <param name="type">消息类型。</param>
        /// <returns>协议 ID，未找到时返回 0。</returns>
        public ushort GetId(Type type) => _typeToId.GetValueOrDefault(type, (ushort)0);

        /// <summary>
        /// 根据泛型消息类型获取其注册的协议 ID。
        /// </summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <returns>协议 ID，未找到时返回 0。</returns>
        public ushort GetId<T>() => _typeToId.GetValueOrDefault(typeof(T), (ushort)0);

        /// <summary>
        /// 根据协议 ID 查找对应的消息类型。
        /// </summary>
        /// <param name="id">协议 ID。</param>
        /// <returns>消息类型，ID 不存在时返回 null。</returns>
        public Type GetType(ushort id) => _idToType.GetValueOrDefault(id);

        /// <summary>
        /// 将协议消息对象序列化为 MessagePack 字节数组。
        /// </summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="message">要序列化的消息实例。</param>
        /// <returns>序列化后的字节数组。</returns>
        public byte[] Serialize<T>(T message)
        {
            return MessagePackSerializer.Serialize(message, _options);
        }

        /// <summary>
        /// 根据协议 ID 将字节载荷反序列化为对应的 IProtocolMessage 实例。
        /// </summary>
        /// <param name="id">协议 ID。</param>
        /// <param name="payload">MessagePack 编码的字节载荷。</param>
        /// <returns>反序列化后的消息实例，类型不存在时返回 null。</returns>
        public IProtocolMessage Deserialize(ushort id, ReadOnlyMemory<byte> payload)
        {
            Type type = GetType(id);
            if (type == null) return null;
            return (IProtocolMessage)MessagePackSerializer.Deserialize(type, payload, _options);
        }

        /// <summary>
        /// 使用指定类型将对象序列化为 MessagePack 字节数组。
        /// </summary>
        /// <param name="type">消息类型。</param>
        /// <param name="obj">要序列化的对象。</param>
        /// <returns>序列化后的字节数组。</returns>
        public byte[] Serialize(Type type, object obj)
        {
            return MessagePackSerializer.Serialize(type, obj, _options);
        }

        /// <summary>
        /// 使用指定类型将字节载荷反序列化为对象实例。
        /// </summary>
        /// <param name="type">目标类型。</param>
        /// <param name="payload">MessagePack 编码的字节载荷。</param>
        /// <returns>反序列化后的对象，空载荷时返回 null。</returns>
        public object Deserialize(Type type, byte[] payload)
        {
            if (payload == null || payload.Length == 0) return null;
            return MessagePackSerializer.Deserialize(type, payload, _options);
        }

        /// <summary>
        /// 关闭协议中心：清空所有协议 ID 与类型的映射关系。
        /// 通常在服务器或客户端关闭时调用。
        /// </summary>
    }
}
