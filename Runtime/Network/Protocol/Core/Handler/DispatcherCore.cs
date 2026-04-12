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


        public static async UniTask DispatchAsync(int channelId, ushort protocolId, IProtocolMessage message)
        {
            await UniTask.SwitchToMainThread();

            if (!_handlerMap.TryGetValue(protocolId, out var handlers)) return;
            if (handlers.Count == 0) return;

            for (int i = handlers.Count - 1; i >= 0; i--) 
            {
                try { await handlers[i].Handle(channelId, message); }
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

                // 客户端签名校验：void Method(IProtocolMessage msg)
                if (param.Length == 1 && typeof(IProtocolMessage).IsAssignableFrom(param[0].ParameterType))
                {
                    msgType = param[0].ParameterType;
                    var actionType = typeof(Action<>).MakeGenericType(msgType);
                    var del = Delegate.CreateDelegate(actionType, target, method);
                    handler = (IMessageHandler)Activator.CreateInstance(typeof(ClientProtocolHandler<>).MakeGenericType(msgType), del);
                }
                // 服务端签名校验：void Method(int channelId, IProtocolMessage msg)
                else if (param.Length == 2 && param[0].ParameterType == typeof(int) && typeof(IProtocolMessage).IsAssignableFrom(param[1].ParameterType))
                {
                    msgType = param[1].ParameterType;
                    var actionType = typeof(Action<,>).MakeGenericType(typeof(int), msgType);
                    var del = Delegate.CreateDelegate(actionType, target, method);
                    handler = (IMessageHandler)Activator.CreateInstance(typeof(ServerProtocolHandler<>).MakeGenericType(msgType), del);
                }
                else
                {
                    LogCore.Warn(nameof(DispatcherCore), $"方法签名错误: {method.Name}");
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