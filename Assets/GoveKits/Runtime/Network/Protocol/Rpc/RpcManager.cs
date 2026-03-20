using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;
using Generated;
using GoveKits.Runtime.Core.Singleton;
using GoveKits.Runtime.Core; // Proto 代码

namespace GoveKits.Runtime.Network.Protocol
{
    [RequireComponent(typeof(NetworkManager))]
    public class RpcManager : MonoSingleton<RpcManager>
    {
        private NetworkClient _client;
        private int _nextRpcId = 1;

        // 核心字典：RpcID -> 等待中的 Task
        // 我们用 object 存储 TaskCompletionSource，因为泛型不同
        private readonly Dictionary<int, object> _pendingRpcs = new Dictionary<int, object>();

        public void Awake()
        {
            _client = NetworkManager.Instance.Client;
            NetworkManager.Instance.Dispatcher.Bind(this);
        }

        /// <summary>
        /// 发起 RPC 调用
        /// </summary>
        /// <typeparam name="TResponse">期望收到的回包类型</typeparam>
        public async UniTask<TResponse> Call<TResponse>(IMessage request, int timeoutMs = 5000) where TResponse : IMessage<TResponse>, new()
        {
            int rpcId = _nextRpcId++;
            
            // 1. 序列化业务消息体（不包含网络层包头）
            int reqMsgId = MessageRegistry.GetId(request.GetType());
            byte[] bodyBytes = request.ToByteArray();

            // 2. 构造信封
            var envelope = new RpcRequest
            {
                RpcId = rpcId,
                TargetMsgId = reqMsgId,
                Payload = ByteString.CopyFrom(bodyBytes)
            };

            // 3. 创建等待源
            var tcs = new UniTaskCompletionSource<TResponse>();
            _pendingRpcs[rpcId] = tcs;

            // 4. 发送
            _client.Send(envelope);

            // 5. 等待结果 (带超时处理)
            try
            {
                // 如果 5秒内没结果，抛出 TimeoutException
                return await tcs.Task.Timeout(TimeSpan.FromMilliseconds(timeoutMs));
            }
            catch (TimeoutException)
            {
                _pendingRpcs.Remove(rpcId);
                LogCore.LogError("RPC", $"Timeout! ID: {rpcId}, Req: {request.GetType().Name}");
                return default;
            }
        }

        /// <summary>
        /// 处理收到的 RPC 回包 (由 NetworkManager/Dispatcher 调用)
        /// </summary>
        [MessageHandler]
        public void HandleResponse(RpcResponse msg)
        {
            if (!_pendingRpcs.TryGetValue(msg.RpcId, out var tcsObj))
            {
                // 可能是超时了，或者 ID 错误
                return;
            }

            _pendingRpcs.Remove(msg.RpcId);

            // 错误处理
            if (msg.ErrorCode != 0)
            {
                Debug.LogError($"[RPC] Server Error: {msg.ErrorCode}");
                // 这里可以选择让 Task 失败，或者返回 null
                // ((IUniTaskCompletionSource)tcsObj).TrySetException(...);
                return;
            }

            // 1. 反序列化内部消息
            // 我们需要知道 TResponse 具体是谁，这里利用闭包或反射
            // 更好的方式：tcsObj 其实是 UniTaskCompletionSource<TResponse>
            
            // 利用 dynamic 或者是反射调用 TrySetResult
            // 因为我们在 Compile Time 不知道 TResponse，但在 Runtime，tcsObj 知道
            
            // 解析 Payload
            var parser = MessageRegistry.GetParser(msg.TargetMsgId);
            if (parser == null) return;

            IMessage responseBody = parser.ParseFrom(msg.Payload);

            // 反射调用 TrySetResult (性能稍低，但在 RPC 频率下可接受，或者优化为 Action 缓存)
            var method = tcsObj.GetType().GetMethod("TrySetResult");
            method.Invoke(tcsObj, new object[] { responseBody });
        }
    }
}