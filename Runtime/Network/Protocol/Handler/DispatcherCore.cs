using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class DispatcherCore
    {
        private static Dictionary<ushort, List<IMessageHandler>> _handlerMap = new();
        private static Dictionary<object, List<(ushort, IMessageHandler)>> _instanceBindings = new();

        public static async UniTask DispatchAsync(Session session, ushort protocolId, IProtocolMessage message)
        {
            await UniTask.SwitchToMainThread();

            if (!_handlerMap.TryGetValue(protocolId, out var handlers)) return;
            if (handlers.Count == 0) return;

            for (int i = handlers.Count - 1; i >= 0; i--) 
            {
                try { await handlers[i].Handle(session, message); }
                catch (Exception ex) { LogCore.Error(nameof(DispatcherCore), $"执行失败: {ex}"); }
            }
        }

        public static void Bind(object target)
        {
            if (_instanceBindings.ContainsKey(target)) return;
            var bindings = new List<(ushort, IMessageHandler)>();

            foreach (var method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<MessageHandlerAttribute>() == null) continue;

                var param = method.GetParameters();
                IMessageHandler handler = null;
                Type msgType = null;

                if (param.Length == 1 && typeof(IProtocolMessage).IsAssignableFrom(param[0].ParameterType))
                {
                    msgType = param[0].ParameterType;
                    var actionType = typeof(Action<>).MakeGenericType(msgType);
                    var del = Delegate.CreateDelegate(actionType, target, method);
                    handler = (IMessageHandler)Activator.CreateInstance(typeof(ClientProtocolHandler<>).MakeGenericType(msgType), del);
                }
                else if (param.Length == 2 && param[0].ParameterType == typeof(Session) && typeof(IProtocolMessage).IsAssignableFrom(param[1].ParameterType))
                {
                    msgType = param[1].ParameterType;
                    var actionType = typeof(Action<,>).MakeGenericType(typeof(Session), msgType);
                    var del = Delegate.CreateDelegate(actionType, target, method);
                    handler = (IMessageHandler)Activator.CreateInstance(typeof(ServerProtocolHandler<>).MakeGenericType(msgType), del);
                }
                else
                {
                    LogCore.Warning(nameof(DispatcherCore), $"方法签名不合法: {method.Name}");
                }

                if (handler != null && msgType != null)
                {
                    var protocolId = ProtocolCore.GetId(msgType);
                    if (protocolId == 0) continue;

                    if (!_handlerMap.ContainsKey(protocolId)) _handlerMap[protocolId] = new();
                    _handlerMap[protocolId].Add(handler);
                    bindings.Add((protocolId, handler));
                }
            }

            if (bindings.Count > 0) _instanceBindings[target] = bindings;
        }

        public static void Unbind(object target)
        {
            if (_instanceBindings.TryGetValue(target, out var list))
            {
                foreach (var (id, h) in list)
                    if (_handlerMap.TryGetValue(id, out var l)) l.Remove(h);
                _instanceBindings.Remove(target);
            }
        }

        public static void Clear() { _handlerMap.Clear(); _instanceBindings.Clear(); }
    }
}