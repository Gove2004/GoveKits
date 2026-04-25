// RpcServerCore.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public static class RpcServerCore
    {
        // 存储注册的 RPC 方法：MethodName -> 包装好的异步委托
        private static readonly Dictionary<string, Func<Session, byte[], UniTask<byte[]>>> _rpcHandlers = new();
        private static readonly RpcServerProxy _proxy = new RpcServerProxy();

        public static void Initialize() => ServerCore.Dispatcher.Bind(_proxy);
        public static void Shutdown() => ServerCore.Dispatcher.Unbind(_proxy);

        /// <summary>
        /// 服务端注册 RPC 处理方法
        /// </summary>
        public static void Register<TReq, TRes>(string methodName, Func<Session, TReq, UniTask<TRes>> handler) 
            where TReq : class 
            where TRes : class
        {
            _rpcHandlers[methodName] = async (session, argsData) =>
            {
                var reqObj = ProtocolCore.Deserialize(typeof(TReq), argsData) as TReq;
                var resObj = await handler(session, reqObj);
                return resObj == null ? null : ProtocolCore.Serialize(typeof(TRes), resObj);
            };
        }

        private class RpcServerProxy
        {
            [MessageHandler]
            public async void OnRpcRequest(Session session, RpcRequestMsg req)
            {
                var response = new RpcResponseMsg { RpcId = req.RpcId };

                if (_rpcHandlers.TryGetValue(req.MethodName, out var handler))
                {
                    try
                    {
                        // 执行业务逻辑
                        response.ReturnData = await handler(session, req.ArgsData);
                    }
                    catch (Exception ex)
                    {
                        response.ErrorCode = 500;
                        response.ErrorMsg = ex.Message;
                    }
                }
                else
                {
                    response.ErrorCode = 404;
                    response.ErrorMsg = $"RPC Method [{req.MethodName}] not found.";
                }

                // 异步返回给客户端
                session.Send(response);
            }
        }
    }
}