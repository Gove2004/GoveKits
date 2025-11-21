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
                var attr = GetType().GetCustomAttribute<NetMessageAttribute>();
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

    public abstract class Message<T> : Message where T : BinaryData, new()
    {
        public T MsgData;
        public Message() { MsgData = new T(); }
        public Message(T data)
        {
            MsgData = data;
        }

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
            
            // 但根据你的逻辑，MessageBuilder Create 后，是直接调 Reading。
            // 所以这里需要按顺序读：
            int readId = ReadInt(buffer, ref index);
            int readLen = ReadInt(buffer, ref index);
            
            MsgData.Reading(buffer, ref index);
        }
    }


    // 自定义属性用于标记消息类的 ID, 并自动注册到 MessageBuilder
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class NetMessageAttribute : Attribute
    {
        public int Id { get; }

        public NetMessageAttribute(int id)
        {
            Id = id;
        }
    }



    public static class MessageBuilder
    {
        private static readonly Dictionary<int, Func<Message>> factories = new();

        /// <summary>
        /// 【旧方式】手动指定 ID 注册
        /// </summary>
        public static void Register<T>(int msgId) where T : Message, new()
        {
            if (factories.ContainsKey(msgId))
            {
                Debug.LogWarning($"[MessageBuilder] ID {msgId} overwritten.");
            }
            factories[msgId] = () => new T();
        }

        /// <summary>
        /// 【新方式】自动读取 [NetMessage] 注册
        /// 用法: MessageBuilder.Register<MsgHeartbeat>();
        /// </summary>
        public static void Register<T>() where T : Message, new()
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<NetMessageAttribute>();
            
            if (attr == null)
            {
                Debug.LogError($"[MessageBuilder] Failed to register {type.Name}: Missing [NetMessage] attribute.");
                return;
            }

            Register<T>(attr.Id);
        }

        /// <summary>
        /// 【究极懒人版】自动扫描当前程序集所有带 [NetMessage] 的类并注册
        /// 用法: MessageBuilder.AutoRegisterAll();
        /// </summary>
        public static void AutoRegisterAll()
        {
            // 获取当前程序集的所有类型
            var allMessages = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Message)) && !t.IsAbstract && t.GetCustomAttribute<NetMessageAttribute>() != null);

            foreach (var type in allMessages)
            {
                var attr = type.GetCustomAttribute<NetMessageAttribute>();
                int id = attr.Id;

                // 创建工厂委托 () => new MsgClass()
                // 因为是反射拿到的 Type，这里用 Activator
                if (!factories.ContainsKey(id))
                {
                    factories[id] = () => (Message)Activator.CreateInstance(type);
                    Debug.Log($"[AutoReg] Registered Msg: {type.Name} ID: {id}");
                }
            }
        }
        
        public static Message Create(int msgId)
        {
            if (factories.TryGetValue(msgId, out var factory)) return factory();
            Debug.LogError($"[MessageBuilder] MsgID {msgId} not registered.");
            return null;
        }
    }
}