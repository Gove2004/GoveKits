using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 消息处理器接口。
    /// 实现此接口以自定义协议消息的处理逻辑。
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 处理接收到的协议消息。
        /// </summary>
        /// <param name="session">消息来源的会话。</param>
        /// <param name="message">协议消息实例。</param>
        /// <returns>表示处理异步操作的 UniTask。</returns>
        UniTask Handle(Session session, IProtocolMessage message);
    }

    /// <summary>
    /// 消息处理器特性，标注在方法上表示该方法处理对应协议的消息。
    /// 配合 MessageDispatcher 使用，通过反射自动发现并注册处理器。
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class MessageHandlerAttribute : System.Attribute { }

    /// <summary>
    /// 通用的 Action 消息处理器，将委托包装为 IMessageHandler 实现。
    /// </summary>
    public class ActionProtocolHandler : IMessageHandler
    {
        private readonly System.Action<Session, IProtocolMessage> _handler;

        /// <summary>
        /// 创建通用 Action 消息处理器。
        /// </summary>
        /// <param name="handler">处理消息的回调委托。</param>
        public ActionProtocolHandler(System.Action<Session, IProtocolMessage> handler) => _handler = handler;

        public UniTask Handle(Session session, IProtocolMessage message)
        {
            _handler(session, message);
            return UniTask.CompletedTask;
        }
    }
}
