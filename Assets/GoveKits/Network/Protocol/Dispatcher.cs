using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GoveKits.Network
{
    // 1. 定义非泛型接口，供 Dispatcher 统一存储
    public interface IMessageHandler
    {
        UniTask Handle(Message message);
    }

    // 2. 泛型基类，自动处理类型转换，开发者只需继承这个
    public class MessageHandler<TMsg> : IMessageHandler where TMsg : Message
    {
        private readonly Action<TMsg> _handlerAction;
        public MessageHandler(Action<TMsg> handlerAction)
        {
            _handlerAction = handlerAction;
        }
        public async UniTask Handle(Message message)
        {
            if (message is TMsg tMsg)
            {
                await Run(tMsg);
            }
            else
            {
                UnityEngine.Debug.LogError($"[MessageHandler] Type mismatch. Handler expects {typeof(TMsg).Name}, got {message.GetType().Name}");
            }
        }

        protected UniTask Run(TMsg msg)
        {
            _handlerAction?.Invoke(msg);
            return UniTask.CompletedTask;
        }
    }


    // 3. 消息分发器
    public class MessageDispatcher
    {
        private readonly Dictionary<int, List<IMessageHandler>> handlers = new();

        public IMessageHandler Register(int msgID, IMessageHandler handler)
        {
            if (!handlers.ContainsKey(msgID))
            {
                handlers[msgID] = new List<IMessageHandler>();
            }
            handlers[msgID].Add(handler);
            return handler;
        }

        public void Unregister(int msgID, IMessageHandler handler)
        {
            if (handlers.TryGetValue(msgID, out var list))
            {
                list.Remove(handler);
            }
        }

        public async UniTask DispatchAsync(Message msg)
        {
            if (msg == null) return;

            if (handlers.TryGetValue(msg.MsgID, out var list))
            {
                // 遍历所有监听者（例如可能有多个系统监听同一个消息）
                foreach (var handler in list)
                {
                    try
                    {
                        await handler.Handle(msg);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[Dispatcher] Error handling MsgID {msg.MsgID}: {ex}");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Dispatcher] No handler found for MsgID {msg.MsgID}");
            }
        }
    }
}