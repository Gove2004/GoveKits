// RpcClientCore.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public static class RpcClientCore
    {
        private static int _nextRpcId = 1;
        private static readonly ConcurrentDictionary<int, Action<RpcResponseMsg>> _pendingRpcs = new();
        private static readonly RpcClientProxy _proxy = new RpcClientProxy();

        public static void Initialize()
        {
            ClientCore.Dispatcher.Bind(_proxy);
            ClientCore.OnDisconnected += HandleDisconnect;
        }

        public static void Shutdown()
        {
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

        private class RpcClientProxy
        {
            [MessageHandler]
            public void OnRpcResponse(RpcResponseMsg msg)
            {
                if (_pendingRpcs.TryRemove(msg.RpcId, out var action)) action.Invoke(msg);
            }
        }
    }
}