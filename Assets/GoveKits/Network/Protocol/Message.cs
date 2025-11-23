using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using GoveKits.Save;
using UnityEngine;

namespace GoveKits.Network
{
    // ===================================================================================
    // 1. 基础数据结构
    // ===================================================================================

    public class MessageHeader : BinaryData
    {
        public int SenderID = 0;  // 发送者的ID，0表示系统
        public int TargetID = 0;  // 接收者的ID，0表示系统
        public override int Length() => 8;
        public override void Reading(byte[] buffer, ref int index)
        {
            SenderID = ReadInt(buffer, ref index);
            TargetID = ReadInt(buffer, ref index);
        }
        public override void Writing(byte[] buffer, ref int index)
        {
            WriteInt(buffer, SenderID, ref index);
            WriteInt(buffer, TargetID, ref index);
        }
    }

    /// <summary>
    /// 标记一个 Message 子类的消息ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageAttribute : Attribute
    {
        public int Id { get; }
        public MessageAttribute(int id) => Id = id;
    }

    // ===================================================================================
    // 2. 消息构建器 (核心修改)
    // ===================================================================================

    /// <summary>
    /// 消息工厂：负责 ID <-> Type 的双向映射与实例化
    /// </summary>
    public static class MessageBuilder
    {
        // ID -> 构造函数 (用于接收时创建实例)
        private static readonly Dictionary<int, Func<Message>> _factories = new();
        
        // Type -> ID (用于发送时 new() 自动填充 ID)
        private static readonly Dictionary<Type, int> _typeToId = new();

        /// <summary>
        /// 注册消息类型
        /// </summary>
        public static void Register(Type messageType, int msgId)
        {
            if (!typeof(Message).IsAssignableFrom(messageType)) return;

            // 1. 注册工厂 (ID -> Msg)
            if (!_factories.ContainsKey(msgId))
            {
                NewExpression newExp = Expression.New(messageType);
                LambdaExpression lambda = Expression.Lambda(typeof(Func<Message>), newExp);
                Func<Message> compiledFactory = (Func<Message>)lambda.Compile();
                _factories[msgId] = compiledFactory;
            }

            // 2. 注册类型映射 (Type -> ID)
            _typeToId[messageType] = msgId;
        }

        /// <summary>
        /// 根据 ID 创建消息实例 (接收时用)
        /// </summary>
        public static T Create<T>(int msgId) where T : Message
        {
            if (_factories.TryGetValue(msgId, out var factory)) 
                return (T)factory();
            return null;
        }

        /// <summary>
        /// 获取消息类型的 ID (发送时构造函数用)
        /// </summary>
        public static int GetMsgID(Type type)
        {
            // 1. 优先从缓存取 (极快)
            if (_typeToId.TryGetValue(type, out int id))
                return id;

            // 2. 缓存没有(可能是没调用 AutoRegisterAll)，尝试现场反射并注册 (懒加载)
            var attr = type.GetCustomAttribute<MessageAttribute>();
            if (attr != null)
            {
                Register(type, attr.Id);
                return attr.Id;
            }

            Debug.LogError($"[MessageBuilder] Type {type.Name} has no [Message(id)] attribute!");
            return -1;
        }

        /// <summary>
        /// 自动注册所有带有 [Message] 特性的类
        /// </summary>
        public static void AutoRegisterAll()
        {
            _factories.Clear();
            _typeToId.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                // 简单的过滤，避免扫描系统程序集 (可选优化)
                if (assembly.GetName().Name.StartsWith("System") || 
                    assembly.GetName().Name.StartsWith("Unity")) continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || !type.IsSubclassOf(typeof(Message))) continue;

                    var attr = type.GetCustomAttribute<MessageAttribute>();
                    if (attr != null)
                    {
                        Register(type, attr.Id);
                    }
                }
            }
            Debug.Log($"[MessageBuilder] Registered {_factories.Count} messages.");
        }
    }

    // ===================================================================================
    // 3. 消息基类
    // ===================================================================================

    public abstract class Message : BinaryData
    {
        public int MsgID = -1; // 消息ID必须是第一个字段
    }

    public abstract class MessageBody : BinaryData { }

    public class Message<T> : Message where T : MessageBody, new()
    {
        public MessageHeader Header = new MessageHeader();
        public T Body = new T();

        // 构造函数
        public Message() => Init();
        public Message(T body) 
        { 
            Init();
            Body = body; 
        }

        // 初始化时直接去 Builder 查表
        private void Init()
        {
            // 这里调用 MessageBuilder.GetMsgID，利用缓存获取 ID
            MsgID = MessageBuilder.GetMsgID(this.GetType());
        }

        public override int Length() => 4 + Header.Length() + Body.Length(); // MsgID(4) + Header + Body

        public override void Writing(byte[] buffer, ref int index)
        {
            WriteInt(buffer, MsgID, ref index); 
            Header.Writing(buffer, ref index);
            Body.Writing(buffer, ref index);
        }

        public override void Reading(byte[] buffer, ref int index)
        {
            MsgID = ReadInt(buffer, ref index);
            Header.Reading(buffer, ref index);
            Body.Reading(buffer, ref index);
        }
    }
}