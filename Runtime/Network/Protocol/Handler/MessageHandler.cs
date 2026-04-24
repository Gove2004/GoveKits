using System;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute { }

    public interface IMessageHandler 
    { 
        UniTask Handle(Session session, IProtocolMessage message); 
    }

    internal class ClientProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
    {
        private readonly Action<TMsg> _action;
        public ClientProtocolHandler(Action<TMsg> action) => _action = action;
        
        public UniTask Handle(Session session, IProtocolMessage message) 
        { 
            if (message is TMsg typed) _action(typed); 
            return UniTask.CompletedTask; 
        }
    }

    internal class ServerProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
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