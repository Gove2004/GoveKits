using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class RpcCore
    {
        private static int _nextRpcId = 1;
        
        // 核心：存的是一个 Action，利用闭包强转类型，完美避开反射！
        private static readonly ConcurrentDictionary<int, Action<IRpcResponse>> _pendingRpcs = new();

        public static void Initialize()
        {
            // 监听底层所有收到的消息
            // ClientCore.OnMessageReceived += HandleIncomingMessage;
            ClientCore.OnDisconnected += HandleDisconnect;
        }

        public static void Shutdown()
        {
            // ClientCore.OnMessageReceived -= HandleIncomingMessage;
            ClientCore.OnDisconnected -= HandleDisconnect;
            
            // 取消所有等待中的 RPC
            foreach (var rpc in _pendingRpcs.Values)
            {
                rpc.Invoke(null); // 传 null 代表强行中断
            }
            _pendingRpcs.Clear();
        }

        /// <summary>
        /// 客户端发起 RPC 调用
        /// </summary>
        public static async UniTask<TResponse> CallAsync<TResponse>(IRpcRequest request, int timeoutMs = 5000) 
            where TResponse : class, IRpcResponse
        {
            if (!ClientCore.IsConnected)
            {
                throw new Exception("RPC Failed: Client is not connected.");
            }

            int rpcId = Interlocked.Increment(ref _nextRpcId);
            request.RpcId = rpcId;

            var tcs = new UniTaskCompletionSource<TResponse>();

            // 存入回调委托（神奇的闭包，避免了反射调用 TrySetResult）
            _pendingRpcs[rpcId] = (response) =>
            {
                if (response == null) 
                {
                    tcs.TrySetException(new Exception("RPC Cancelled (Disconnected)"));
                    return;
                }

                if (response.ErrorCode != 0)
                {
                    tcs.TrySetException(new Exception($"RPC Server Error [{response.ErrorCode}]: {response.ErrorMsg}"));
                    return;
                }

                if (response is TResponse typedRes)
                {
                    tcs.TrySetResult(typedRes);
                }
                else
                {
                    tcs.TrySetException(new Exception($"RPC Type Mismatch! Expected {typeof(TResponse).Name}, got {response.GetType().Name}"));
                }
            };

            // 1. 发送请求
            ClientCore.Send(request);

            // 2. 超时处理（UniTask 的完美 Timeout 写法，不漏内存）
            var timeoutTask = UniTask.Delay(timeoutMs);
            var (isTimeout, _) = await UniTask.WhenAny(tcs.Task, timeoutTask);

            if (isTimeout)
            {
                _pendingRpcs.TryRemove(request.RpcId, out _);
                throw new TimeoutException($"RPC timeout after {timeoutMs}ms");
            }

            // 3. 返回正确结果
            return await tcs.Task;
        }

        /// <summary>
        /// 拦截底层的网络消息，如果是 RPC 响应，就触发回调
        /// </summary>
        private static void HandleIncomingMessage(ushort protocolId, IProtocolMessage msg)
        {
            if (msg is IRpcResponse response)
            {
                if (_pendingRpcs.TryRemove(response.RpcId, out var action))
                {
                    // 触发上面 CallAsync 里的闭包
                    action.Invoke(response);
                }
            }
        }

        private static void HandleDisconnect(string reason)
        {
            Shutdown();
        }
    }
}