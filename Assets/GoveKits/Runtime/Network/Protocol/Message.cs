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
    // 消息基类
    // ===================================================================================
    public abstract class Message : BinaryData
    {
        // 协议固定结构
        public int MsgID { get; protected set; } = -1;
        public MessageHeader Header = new MessageHeader();

        public Message()
        {
            // 构造时自动获取ID
            MsgID = MessageBuilder.GetMsgID(this.GetType());
        }

        // ========================================================
        // 1. 密封主流程：防止子类破坏协议头结构 (ID + Header + Body)
        // ========================================================

        public override sealed int Length()
        {
            // 总长度 = ID(4) + Header(8) + Body长度
            return 4 + Header.Length() + BodyLength(); 
        }

        public override sealed void Writing(byte[] buffer, ref int index)
        {
            // 1. 写 ID
            WriteInt(buffer, MsgID, ref index);
            // 2. 写 Header
            Header.Writing(buffer, ref index);
            // 3. 写 Body (子类实现)
            BodyWriting(buffer, ref index);
        }

        public override sealed void Reading(byte[] buffer, ref int index)
        {
            // 1. 读 ID
            MsgID = ReadInt(buffer, ref index);
            // 2. 读 Header
            Header.Reading(buffer, ref index);
            // 3. 读 Body (子类实现)
            BodyReading(buffer, ref index);
        }

        // ========================================================
        // 2. 子类必须实现的接口 (只关心自己的数据)
        // ========================================================

        /// <summary>
        /// 子类数据的长度 (不包含 Header 和 ID)
        /// </summary>
        protected abstract int BodyLength();

        /// <summary>
        /// 序列化子类字段
        /// </summary>
        protected abstract void BodyWriting(byte[] buffer, ref int index);

        /// <summary>
        /// 反序列化子类字段
        /// </summary>
        protected abstract void BodyReading(byte[] buffer, ref int index);
    }
    
    // ===================================================================================
    // 如果你有一些不需要数据的空消息（如心跳），可以搞个基类方便继承
    // ===================================================================================
    public abstract class EmptyMessage : Message
    {
        protected override int BodyLength() => 0;
        protected override void BodyWriting(byte[] buffer, ref int index) { }
        protected override void BodyReading(byte[] buffer, ref int index) { }
    }

    
    // 同步消息基类
    public abstract class SyncMessage: Message
    {
        public int NetID;  // 目标对象的网络ID
        // 
        protected override int BodyLength() => 4 + SyncDataLength(); // NetID(4) + 数据长度
        protected override void BodyWriting(byte[] buffer, ref int index)
        {
            WriteInt(buffer, NetID, ref index);  // 先写 NetID
            SyncDataWriting(buffer, ref index);  // 再写数据
        }
        protected override void BodyReading(byte[] buffer, ref int index)
        {
            NetID = ReadInt(buffer, ref index);  // 先读 NetID
            SyncDataReading(buffer, ref index);  // 再读数据
        }
        // 子类
        protected abstract int SyncDataLength();
        protected abstract void SyncDataWriting(byte[] buffer, ref int index);
        protected abstract void SyncDataReading(byte[] buffer, ref int index);
    }
}