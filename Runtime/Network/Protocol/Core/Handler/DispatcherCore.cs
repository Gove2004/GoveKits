using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;


namespace GoveKits.Runtime.Network
{
    public class DispatcherCore : ICore
    {
        private readonly Dictionary<ushort, List<IMessageHandler>> _handlerMap = new();
        private readonly Dictionary<object, List<(ushort, IMessageHandler)>> _instanceBindings = new();


        public async UniTask DispatchAsync(int channelId, ushort protocolId, IProtocolMessage message)
        {
            await UniTask.SwitchToMainThread();

            if (!_handlerMap.TryGetValue(protocolId, out var handlers)) return;

            for (int i = handlers.Count - 1; i >= 0; i--) 
            {
                try { await handlers[i].Handle(channelId, message); }
                catch (Exception ex) { CoreLocator.Log.Error("Dispatcher", $"执行失败: {ex}"); }
            }
        }

        public void Bind(object target)
        {
            if (_instanceBindings.ContainsKey(target)) return;

            var bindings = new List<(ushort, IMessageHandler)>();
            var protocolCore = CoreLocator.GetCore<ProtocolCore>();

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

                if (handler != null && msgType != null)
                {
                    var protocolId = protocolCore.GetId(msgType);
                    if (protocolId == 0) continue;

                    if (!_handlerMap.ContainsKey(protocolId)) _handlerMap[protocolId] = new();
                    _handlerMap[protocolId].Add(handler);
                    bindings.Add((protocolId, handler));
                }
            }

            if (bindings.Count > 0) _instanceBindings[target] = bindings;
        }

        public void Unbind(object target)
        {
            if (_instanceBindings.TryGetValue(target, out var list))
            {
                foreach (var (id, h) in list)
                    if (_handlerMap.TryGetValue(id, out var l)) l.Remove(h);
                _instanceBindings.Remove(target);
            }
        }
        public void OnShutdown() { _handlerMap.Clear(); _instanceBindings.Clear(); }
    }
}