using System;
using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;
using GoveKits.Manager;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetManager : MonoSingleton<NetManager>
    {
        [Header("Settings")]
        public string RemoteIP = "127.0.0.1";
        public int RemotePort = 12345;

        // --- 组件 ---
        private NetSocket _socket;
        private PacketParser _parser;
        private MessageDispatcher _dispatcher;

        // --- 状态 ---
        // 线程安全队列：用于将后台线程解析好的消息传递给主线程
        private readonly ConcurrentQueue<Message> _msgQueue = new ConcurrentQueue<Message>();
        
        public bool IsConnected => _socket != null && _socket.IsConnected;

        public event Action OnConnected;
        public event Action OnDisconnected;

        private void Awake()
        {
            MessageBuilder.AutoRegisterAll(); // 假设这是你的消息注册逻辑
            InitializePipeline();
            Connect();
        }

        // ★★★ 核心改动：流水线组装 ★★★
        private void InitializePipeline()
        {
            _socket = new NetSocket();
            _parser = new PacketParser();
            _dispatcher = new MessageDispatcher();

            // 1. Socket 接收原始数据 -> 喂给 Parser
            _socket.OnReceiveData += _parser.InputRawData;

            // 2. Socket 状态事件转发
            _socket.OnConnected += () => 
            {
                Debug.Log("[NetManager] Socket Connected.");
                OnConnected?.Invoke();
            };
            _socket.OnDisconnected += () => 
            {
                Debug.Log("[NetManager] Socket Disconnected.");
                OnDisconnected?.Invoke();
            };

            // 3. Parser 解析出完整消息 -> 放入主线程队列
            _parser.OnMessageReady += (msg) =>
            {
                _msgQueue.Enqueue(msg);
            };
        }

        // --- 生命周期与主线程调度 ---

        private void Update()
        {
            // 4. 主线程从队列取出消息 -> 交给 Dispatcher 分发
            while (_msgQueue.TryDequeue(out Message msg))
            {
                _dispatcher.DispatchAsync(msg).Forget();
            }
        }

        // --- 对外接口 ---

        public void Connect()
        {
            if (IsConnected) return;
            _socket.ConnectAsync(RemoteIP, RemotePort).Forget();
        }

        public void Send(Message msg)
        {
            if (!IsConnected) return;
            // 序列化逻辑建议也可以封装，但这里简单调用
            _socket.SendAsync(msg.Pack()).Forget();
        }

        public void Close() => _socket.Close();

        // 代理 Dispatcher 的注册接口
        public IMessageHandler Register(int msgId, IMessageHandler handler) => _dispatcher.Register(msgId, handler);
        public void Unregister(int msgId, IMessageHandler handler) => _dispatcher.Unregister(msgId, handler);

        protected override void OnDestroy()
        {
            Close();
            _socket?.Dispose();
            base.OnDestroy();
        }
    }
}