using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 消息分发器。
    /// 通过反射绑定标注 [MessageHandler] 的方法，将协议 ID 映射到处理器列表。
    /// 分发时自动切换到主线程执行。
    /// </summary>
    public sealed class MessageDispatcher
    {
        private readonly ProtocolCenter _protocol;
        private readonly Dictionary<ushort, List<IMessageHandler>> _handlerMap = new();
        private readonly Dictionary<object, List<(ushort, IMessageHandler)>> _instanceBindings = new();

        /// <summary>
        /// 创建消息分发器。
        /// </summary>
        /// <param name="protocol">协议中心，用于解析消息类型到协议 ID 的映射。</param>
        public MessageDispatcher(ProtocolCenter protocol = null)
        {
            _protocol = protocol;
        }

        /// <summary>
        /// 异步分发消息到所有注册的处理器。
        /// </summary>
        public async UniTask DispatchAsync(Session session, ushort protocolId, IProtocolMessage message)
        {
            await UniTask.SwitchToMainThread();

            if (!_handlerMap.TryGetValue(protocolId, out var handlers)) return;
            if (handlers.Count == 0) return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                try { await handlers[i].Handle(session, message); }
                catch (Exception ex)
                {
                    var root = ex;
                    while (root.InnerException != null) root = root.InnerException;
                    LogCore.Error(nameof(MessageDispatcher),
                        $"处理失败 protocolId={protocolId}, msgType={message?.GetType().Name}, handler={handlers[i].GetType().Name}, error={root.Message}");
                }
            }
        }

        /// <summary>
        /// 绑定目标对象上标注 [MessageHandler] 的方法。
        /// </summary>
        public void Bind(object target)
        {
            if (_instanceBindings.ContainsKey(target)) return;
            if (_protocol == null)
            {
                LogCore.Warning(nameof(MessageDispatcher), "ProtocolCenter 为 null，无法解析消息处理器对应的协议 ID。");
                return;
            }
            var bindings = new List<(ushort, IMessageHandler)>();

            foreach (var method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<MessageHandlerAttribute>() == null) continue;

                var param = method.GetParameters();
                IMessageHandler handler = null;
                Type msgType = null;

                // 客户端签名: void OnMsg(SomeMsg msg)
                if (param.Length == 1 && typeof(IProtocolMessage).IsAssignableFrom(param[0].ParameterType))
                {
                    msgType = param[0].ParameterType;
                    var actionType = typeof(Action<>).MakeGenericType(msgType);
                    var del = Delegate.CreateDelegate(actionType, target, method);
                    handler = (IMessageHandler)Activator.CreateInstance(typeof(ClientProtocolHandler<>).MakeGenericType(msgType), del);
                }
                // 服务端签名: void OnMsg(Session session, SomeMsg msg)
                else if (param.Length == 2 && param[0].ParameterType == typeof(Session) && typeof(IProtocolMessage).IsAssignableFrom(param[1].ParameterType))
                {
                    msgType = param[1].ParameterType;
                    var actionType = typeof(Action<,>).MakeGenericType(typeof(Session), msgType);
                    var del = Delegate.CreateDelegate(actionType, target, method);
                    handler = (IMessageHandler)Activator.CreateInstance(typeof(ServerProtocolHandler<>).MakeGenericType(msgType), del);
                }

                if (handler != null && msgType != null)
                {
                    var protocolId = _protocol?.GetId(msgType) ?? 0;
                    if (protocolId == 0) continue;

                    if (!_handlerMap.ContainsKey(protocolId)) _handlerMap[protocolId] = new();
                    _handlerMap[protocolId].Add(handler);
                    bindings.Add((protocolId, handler));
                }
                else if (param.Length > 0)
                {
                    LogCore.Warning(nameof(MessageDispatcher),
                        $"[MessageHandler] 方法 {target.GetType().Name}.{method.Name} 签名不匹配，期望 (Session, Msg) 或 (Msg)");
                }
            }

            if (bindings.Count > 0) _instanceBindings[target] = bindings;
        }

        /// <summary>
        /// 解绑目标对象。
        /// </summary>
        public void Unbind(object target)
        {
            if (_instanceBindings.TryGetValue(target, out var list))
            {
                foreach (var (id, h) in list)
                    if (_handlerMap.TryGetValue(id, out var l)) l.Remove(h);
                _instanceBindings.Remove(target);
            }
        }

        /// <summary>
        /// 清空所有处理器。
        /// </summary>
        public void Clear() { _handlerMap.Clear(); _instanceBindings.Clear(); }

        private class ClientProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
        {
            private readonly Action<TMsg> _action;
            public ClientProtocolHandler(Action<TMsg> action) => _action = action;
            public UniTask Handle(Session session, IProtocolMessage message)
            {
                if (message is TMsg typed) _action(typed);
                return UniTask.CompletedTask;
            }
        }

        private class ServerProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
        {
            private readonly Action<Session, TMsg> _action;
            public ServerProtocolHandler(Action<Session, TMsg> action) => _action = action;
            public UniTask Handle(Session session, IProtocolMessage message)
            {
                if (message is TMsg typed) _action(session, typed);
                return UniTask.CompletedTask;
            }
        }
    }
}
