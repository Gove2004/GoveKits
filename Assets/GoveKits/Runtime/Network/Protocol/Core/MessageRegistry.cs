using System;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;


namespace GoveKits.Network
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PacketIDAttribute : Attribute
    {
        public int Id { get; }
        public PacketIDAttribute(int id) => Id = id;
    }



    public static class MessageRegistry
    {
        // ID -> 解析器 (接收用)
        private static readonly Dictionary<int, MessageParser> _parsers = new Dictionary<int, MessageParser>();
        
        // Type -> ID (发送用)
        private static readonly Dictionary<Type, int> _ids = new Dictionary<Type, int>();

        /// <summary>
        /// [反射专用] 非泛型注册接口
        /// </summary>
        public static void Register(int id, Type type, MessageParser parser)
        {
            if (_parsers.ContainsKey(id))
            {
                LogManager.LogWarning("MessageRegistry", $"ID Conflict: {id}");
                return;
            }
            _parsers[id] = parser;
            _ids[type] = id;
        }

        // 获取解析器
        public static MessageParser GetParser(int id) => _parsers.TryGetValue(id, out var p) ? p : null;
        
        // 获取 ID
        public static int GetId(Type type) => _ids.TryGetValue(type, out var id) ? id : -1;
        
        // 泛型获取 ID 便捷方法
        public static int GetId<T>() => GetId(typeof(T));
    

        /// <summary>
        /// 扫描并注册协议
        /// </summary>
        /// <typeparam name="TEnum">协议 ID 枚举</typeparam>
        /// <param name="namespaceName">
        /// 消息类所在的命名空间。
        /// 如果为 null，默认认为 Message 类和 TEnum 在同一个命名空间下。
        /// </param>
        public static void ScanAndRegister<TEnum>(string namespaceName = null) where TEnum : Enum
        {
            Type enumType = typeof(TEnum);
            Assembly assembly = enumType.Assembly;
            
            // 如果未指定命名空间，则使用枚举的命名空间
            if (string.IsNullOrEmpty(namespaceName))
            {
                namespaceName = enumType.Namespace;
            }

            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);

            int count = 0;

            for (int i = 0; i < names.Length; i++)
            {
                string msgName = names[i];
                int msgId = (int)values.GetValue(i);

                // 忽略 0 或负数 (通常作为占位符)
                if (msgId <= 0) continue;

                // 1. 拼凑全类名
                string fullClassName = string.IsNullOrEmpty(namespaceName) 
                    ? msgName 
                    : $"{namespaceName}.{msgName}";

                // 2. 反射获取类型 (注意：必须在同一个 Assembly)
                Type msgType = assembly.GetType(fullClassName);

                if (msgType == null)
                {
                    // 只是警告，可能枚举里定义了但没写 message，或者拼写不一致
                    LogManager.LogWarning("MessageRegistry", $"Class '{fullClassName}' not found for ID {msgId}.");
                    continue;
                }

                // 3. 验证是否是 IMessage
                if (!typeof(IMessage).IsAssignableFrom(msgType)) continue;

                // 4. 获取静态 Parser 属性
                var parserProp = msgType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
                if (parserProp == null) continue;

                var parser = parserProp.GetValue(null) as MessageParser;

                // 5. 注册
                MessageRegistry.Register(msgId, msgType, parser);
                count++;
            }

            LogManager.LogGreen("MessageRegistry", $"Registered {count} messages from '{enumType.Name}' (Namespace: {namespaceName})");
        }
    }
}