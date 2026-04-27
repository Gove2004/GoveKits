
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class RpcCore
    {
        private static int _nextRpcId = 1;
        private static readonly ConcurrentDictionary<int, Action<RpcResponseMsg>> _pendingRpcs = new();
        private static readonly RpcProxy _proxy = new();

        /// <summary>
        /// 客户端初始化，绑定 RPC 消息处理器
        /// </summary>
        public static void Initialize()
        {
            ClientCore.Dispatcher.Bind(_proxy);
            ClientCore.OnDisconnected += HandleDisconnect;

            // 出生和消亡 服务
            // Register<SpawnReq, SpawnRsp>("Spawn", _proxy.OnSpawn);
            // Register<DespawnReq, DespawnRsp>("Despawn", _proxy.OnDespawn);
        }

        /// <summary>
        /// 客户端断开连接时清理状态，取消所有待处理的 RPC 调用
        /// </summary>
        public static void Shutdown()
        {
            // Unregister("Spawn");
            // Unregister("Despawn");

            ClientCore.Dispatcher.Unbind(_proxy);
            ClientCore.OnDisconnected -= HandleDisconnect;
            foreach (var rpc in _pendingRpcs.Values) rpc.Invoke(null);
            _pendingRpcs.Clear();
        }

        /// <summary>
        /// 客户端发起 RPC 调用
        /// </summary>
        public static async UniTask<TRes> CallAsync<TReq, TRes>(string methodName, TReq args, int timeoutMs = 5000) 
            where TReq : class 
            where TRes : class
        {
            if (!ClientCore.IsConnected) throw new Exception("RPC Failed: Not connected.");

            int rpcId = Interlocked.Increment(ref _nextRpcId);
            var tcs = new UniTaskCompletionSource<TRes>();

            // 存入回调
            _pendingRpcs[rpcId] = (response) =>
            {
                if (response == null) { tcs.TrySetException(new Exception("RPC Cancelled")); return; }
                if (response.ErrorCode != 0) { tcs.TrySetException(new Exception($"RPC Error: {response.ErrorMsg}")); return; }
                
                // 解析盲盒返回值
                var resObj = ProtocolCore.Deserialize(typeof(TRes), response.ReturnData) as TRes;
                tcs.TrySetResult(resObj);
            };

            // 发送请求
            ClientCore.Send(new RpcRequestMsg
            {
                RpcId = rpcId,
                MethodName = methodName,
                ArgsData = args == null ? null : ProtocolCore.Serialize(typeof(TReq), args)
            });

            // 超时处理
            var (isTimeout, _) = await UniTask.WhenAny(tcs.Task, UniTask.Delay(timeoutMs));
            if (isTimeout)
            {
                _pendingRpcs.TryRemove(rpcId, out _);
                throw new TimeoutException($"RPC [{methodName}] timeout.");
            }

            return await tcs.Task;
        }

        private static void HandleDisconnect(string reason) => Shutdown();





        // 存储注册的 RPC 方法：MethodName -> 包装好的异步委托
        private static readonly Dictionary<string, Func<Session, byte[], UniTask<byte[]>>> _rpcHandlers = new();

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
        public static void Unregister(string methodName) => _rpcHandlers.Remove(methodName);





        private class RpcProxy
        {
            [MessageHandler]
            public void OnRpcResponse(RpcResponseMsg msg)
            {
                if (_pendingRpcs.TryRemove(msg.RpcId, out var action)) action.Invoke(msg);
            }

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