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
    

    public class ActionProtocolHandler : IMessageHandler
    {
        private readonly Action<Session, IProtocolMessage> _handler;

        public ActionProtocolHandler(Action<Session, IProtocolMessage> handler) => _handler = handler;

        public UniTask Handle(Session session, IProtocolMessage message)
        {
            _handler(session, message);
            return UniTask.CompletedTask;
        }
    }
}