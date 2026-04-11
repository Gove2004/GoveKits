using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute { }

    // 统一处理器接口，带上 ChannelId
    public interface IMessageHandler 
    { 
        UniTask Handle(int channelId, IProtocolMessage message); 
    }

    // 支持 Client 端签名: void OnMsg(TMsg msg)
    public class ClientProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
    {
        private readonly Action<TMsg> _action;
        public ClientProtocolHandler(Action<TMsg> action) => _action = action;
        public UniTask Handle(int channelId, IProtocolMessage message) 
        { 
            if (message is TMsg typed) _action(typed); 
            return UniTask.CompletedTask; 
        }
    }

    // 支持 Server 端签名: void OnMsg(int channelId, TMsg msg)
    public class ServerProtocolHandler<TMsg> : IMessageHandler where TMsg : IProtocolMessage
    {
        private readonly Action<int, TMsg> _action;
        public ServerProtocolHandler(Action<int, TMsg> action) => _action = action;
        public UniTask Handle(int channelId, IProtocolMessage message) 
        { 
            if (message is TMsg typed) _action(channelId, typed); 
            return UniTask.CompletedTask; 
        }
    }
}