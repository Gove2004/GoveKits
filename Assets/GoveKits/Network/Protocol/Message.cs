using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GoveKits.Network
{
    public abstract class Message : BinaryData
    {
        // 缓存 ID，避免每次序列化都反射，提升性能
        private int? _cachedId;
        public virtual int MsgID
        {
            get
            {
                if (_cachedId.HasValue) return _cachedId.Value;

                // 反射获取类上的 [NetMessage] 特性
                var attr = GetType().GetCustomAttribute<MessageAttribute>();
                if (attr != null)
                {
                    _cachedId = attr.Id;
                    return _cachedId.Value;
                }

                throw new Exception($"Class [{GetType().Name}] is missing [NetMessage] attribute!");
            }
        }
        public const int HeaderSize = 8;

        // 新增：打包入口，只在这里分配一次数组
        public byte[] Pack()
        {
            int len = Length();
            byte[] buffer = new byte[len];
            int index = 0;
            Writing(buffer, ref index);
            return buffer;
        }
    }


    public class Message<T> : Message where T : BinaryData, new()
    {
        public T MsgData;

        public Message() => MsgData = new T();
        public Message(T data) => MsgData = data;

        public override int Length() => HeaderSize + MsgData.Length();

        // 统一为 Writing，不再 new byte[]
        public override void Writing(byte[] buffer, ref int index)
        {
            WriteInt(buffer, MsgID, ref index);
            WriteInt(buffer, MsgData.Length(), ref index);
            MsgData.Writing(buffer, ref index); // 递归直接写入，零拷贝
        }

        public override void Reading(byte[] buffer, ref int index)
        {
            // ID 和 Length 已经在外部或者 Header 处理时读过了
            // 如果你是直接丢 Message 进去解包，需要手动读一下跳过，或者由外部控制
            // 假设外部已经读了 ID 和 Length 才知道是这个消息：

            // int readId = ReadInt(buffer, ref index);
            // int readLen = ReadInt(buffer, ref index);
            
            MsgData.Reading(buffer, ref index);
        }
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MessageAttribute : Attribute
    {
        public int Id { get; }
        public MessageAttribute(int id) => Id = id;
    }



    public static class MessageBuilder
    {
        private static readonly Dictionary<int, Func<Message>> factories = new();

        /// <summary>
        /// 通过 Type 注册（用于反射注册）
        /// </summary>
        public static void Register(Type messageType, int msgId)
        {
            if (!typeof(Message).IsAssignableFrom(messageType))
            {
                Debug.LogError($"[MessageBuilder] Type {messageType.FullName} is not a Message.");
                return;
            }

            if (factories.ContainsKey(msgId))
            {
                Debug.LogWarning($"[MessageBuilder] ID {msgId} overwritten.");
            }

            factories[msgId] = () => (Message)Activator.CreateInstance(messageType);
        }

        
        /// <summary>
        /// 通过 MsgID 创建 Message 实例
        /// </summary>
        /// <param name="msgId"></param>
        /// <returns></returns>
        public static T Create<T>(int msgId) where T : Message
        {
            if (factories.TryGetValue(msgId, out var factory)) return (T)factory();
            Debug.LogError($"[MessageBuilder] MsgID {msgId} not registered.");
            return null;
        }


        // 自动注册所有带有 MessageAttribute 的 Message 子类
        public static void AutoRegisterAll()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(Message))) continue;
                
                var attr = type.GetCustomAttribute<MessageAttribute>();
                if (attr != null)
                {
                    MessageBuilder.Register(type, attr.Id);
                }
            }
        }
    }
}