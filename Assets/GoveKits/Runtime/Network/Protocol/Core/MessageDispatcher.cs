using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute { }

    public interface IMessageHandler 
    { 
        UniTask Handle(Message message); 
    }

    public class MessageHandler<TMsg> : IMessageHandler where TMsg : Message
    {
        private readonly Action<TMsg> _action;
        public MessageHandler(Action<TMsg> action) => _action = action;
        
        public UniTask Handle(Message message) 
        { 
            if (message is TMsg t) _action(t); 
            return UniTask.CompletedTask; 
        }
    }

    public class MessageDispatcher
    {
        private readonly Dictionary<int, List<IMessageHandler>> _msgMap = new();
        private readonly Dictionary<object, List<(int, IMessageHandler)>> _targetMap = new();

        public async UniTask DispatchAsync(Message msg)
        {
            // 确保切回主线程
            await UniTask.SwitchToMainThread();
            
            if (_msgMap.TryGetValue(msg.MsgID, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--) 
                    await list[i].Handle(msg);
            }
        }

        public void Bind(object target)
        {
            if (_targetMap.ContainsKey(target)) return;
            var bindings = new List<(int, IMessageHandler)>();

            foreach (var method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<MessageHandlerAttribute>() == null) continue;

                var param = method.GetParameters();
                
                // 签名检查：只接受 (MessageType)
                if (param.Length != 1 || !typeof(Message).IsAssignableFrom(param[0].ParameterType)) 
                {
                    Debug.LogWarning($"[Dispatcher] Invalid Signature: {method.Name}. Expected (Message).");
                    continue;
                }

                Type msgType = param[0].ParameterType;
                int msgId = MessageBuilder.GetMsgID(msgType);
                if (msgId == -1) continue;

                Type actionType = typeof(Action<>).MakeGenericType(msgType);
                Delegate d = Delegate.CreateDelegate(actionType, target, method);

                Type handlerType = typeof(MessageHandler<>).MakeGenericType(msgType);
                var handler = (IMessageHandler)Activator.CreateInstance(handlerType, d);

                if (!_msgMap.ContainsKey(msgId)) _msgMap[msgId] = new List<IMessageHandler>();
                _msgMap[msgId].Add(handler);
                bindings.Add((msgId, handler));
            }
            if(bindings.Count > 0) _targetMap[target] = bindings;
        }

        public void Unbind(object target)
        {
            if (_targetMap.TryGetValue(target, out var list))
            {
                foreach (var (id, h) in list)
                    if (_msgMap.TryGetValue(id, out var l)) l.Remove(h);
                _targetMap.Remove(target);
            }
        }
    }
}