// Dispatch/MessageDispatcher.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 消息处理器标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute { }

    /// <summary>
    /// 处理器接口
    /// </summary>
    public interface IMessageHandler 
    { 
        UniTask Handle(IProtocolMessage message); 
    }

    /// <summary>
    /// 类型安全的泛型处理器包装
    /// </summary>
    public class ProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
    {
        private readonly Action<TMsg> _action;
        public ProtocolHandler(Action<TMsg> action) => _action = action;
        
        public UniTask Handle(IProtocolMessage message) 
        { 
            if (message is TMsg typed) _action(typed); 
            return UniTask.CompletedTask; 
        }
    }

    /// <summary>
    /// 协议消息分发器
    /// 职责：将收到的消息路由到对应的处理器，并确保在主线程执行
    /// </summary>
    public class MessageDispatcher
    {
        // ProtocolId -> 处理器列表（支持多个处理器订阅同一消息）
        private readonly Dictionary<ushort, List<IMessageHandler>> _handlerMap = new();
        
        // 实例 -> 绑定的处理器记录（用于解绑）
        private readonly Dictionary<object, List<(ushort, IMessageHandler)>> _instanceBindings = new();

        /// <summary>
        /// 分发消息到对应处理器（自动切回主线程）
        /// </summary>
        public async UniTask DispatchAsync(IProtocolMessage message)
        {
            await UniTask.SwitchToMainThread();
            
            var protocolId = ProtocolRegistry.GetId(message.GetType());
            if (protocolId == 0)
            {
                Debug.LogWarning($"[MessageDispatcher] 未注册的消息类型: {message.GetType().Name}");
                return;
            }

            if (!_handlerMap.TryGetValue(protocolId, out var handlers))
            {
                Debug.LogWarning($"[MessageDispatcher] 无处理器订阅消息: {protocolId}");
                return;
            }

            // 倒序遍历，允许处理器中解绑自身
            for (int i = handlers.Count - 1; i >= 0; i--) 
            {
                try
                {
                    await handlers[i].Handle(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MessageDispatcher] 处理器执行失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 手动注册处理器（代码方式）
        /// </summary>
        public void Subscribe<TMsg>(Action<TMsg> handler) where TMsg : IProtocolMessage
        {
            var protocolId = ProtocolRegistry.GetId<TMsg>();
            if (protocolId == 0)
            {
                Debug.LogError($"[MessageDispatcher] 未注册的消息类型: {typeof(TMsg).Name}");
                return;
            }

            var wrapper = new ProtocolHandler<TMsg>(handler);
            
            if (!_handlerMap.ContainsKey(protocolId)) 
                _handlerMap[protocolId] = new List<IMessageHandler>();
            
            _handlerMap[protocolId].Add(wrapper);
            
            // 记录到匿名绑定（key 用 handler 本身）
            var key = (object)handler;
            if (!_instanceBindings.ContainsKey(key)) 
                _instanceBindings[key] = new List<(ushort, IMessageHandler)>();
            _instanceBindings[key].Add((protocolId, wrapper));
        }

        /// <summary>
        /// 手动注销处理器
        /// </summary>
        public void Unsubscribe<TMsg>(Action<TMsg> handler) where TMsg : IProtocolMessage
        {
            if (_instanceBindings.Remove(handler, out var bindings))
            {
                foreach (var (id, h) in bindings)
                {
                    if (_handlerMap.TryGetValue(id, out var list)) 
                        list.Remove(h);
                }
            }
        }

        /// <summary>
        /// 自动扫描并绑定实例中标记 [ProtocolHandler] 的方法
        /// </summary>
        public void Bind(object target)
        {
            if (_instanceBindings.ContainsKey(target)) 
            {
                Debug.LogWarning($"[MessageDispatcher] 实例已绑定: {target.GetType().Name}");
                return;
            }

            var bindings = new List<(ushort, IMessageHandler)>();
            var type = target.GetType();

            foreach (var method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<MessageHandlerAttribute>() == null) 
                    continue;

                var param = method.GetParameters();
                if (param.Length != 1 || !typeof(IProtocolMessage).IsAssignableFrom(param[0].ParameterType)) 
                {
                    CoreLocator.Log.Warn("Dispatcher", $"方法签名无效: {type.Name}.{method.Name}(TMsg)");
                    continue;
                }

                var msgType = param[0].ParameterType;
                var protocolId = ProtocolRegistry.GetId(msgType);
                
                if (protocolId == 0) 
                {
                    CoreLocator.Log.Warn("Dispatcher", $"消息类型未注册: {msgType.Name}");
                    continue;
                }

                // 创建委托和包装器
                var actionType = typeof(Action<>).MakeGenericType(msgType);
                var del = Delegate.CreateDelegate(actionType, target, method);
                
                var handlerType = typeof(ProtocolHandler<>).MakeGenericType(msgType);
                var handler = (IMessageHandler)Activator.CreateInstance(handlerType, del);

                if (!_handlerMap.ContainsKey(protocolId)) 
                    _handlerMap[protocolId] = new List<IMessageHandler>();
                
                _handlerMap[protocolId].Add(handler);
                bindings.Add((protocolId, handler));
                
                CoreLocator.Log.Info("Dispatcher", $"绑定: {type.Name}.{method.Name} -> {msgType.Name}({protocolId})");
            }

            if (bindings.Count > 0) 
                _instanceBindings[target] = bindings;
        }

        /// <summary>
        /// 解绑实例的所有处理器
        /// </summary>
        public void Unbind(object target)
        {
            if (!_instanceBindings.Remove(target, out var bindings)) return;
            
            foreach (var (id, handler) in bindings)
            {
                if (_handlerMap.TryGetValue(id, out var list)) 
                    list.Remove(handler);
            }
            
            CoreLocator.Log.Info("Dispatcher", $"解绑: {target.GetType().Name}");
        }

        /// <summary>
        /// 清空所有处理器
        /// </summary>
        public void Clear()
        {
            _handlerMap.Clear();
            _instanceBindings.Clear();
        }
    }
}