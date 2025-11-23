using System;
using Cysharp.Threading.Tasks;


namespace GoveKits.Network
{
    public class NetworkClient : IDisposable
    {
        // 组件
        public readonly ByteSocket Socket;
        public readonly PacketParser Parser;
        public readonly MessageDispatcher Dispatcher;

        // 状态
        public bool IsConnected => Socket != null && Socket.IsConnected;

        public NetworkClient()
        {
            // 1. 创建 分发器
            Dispatcher = new MessageDispatcher();

            // 2. 创建 解析器 (注入分发逻辑：解析器产出消息 -> 分发器处理)
            Parser = new PacketParser(Dispatcher.DispatchAsync);

            // 3. 创建 Socket (注入数据流逻辑：Socket收到字节 -> 解析器处理)
            Socket = new ByteSocket(Parser.InputRawData);
            
            // 自动注册消息类型
            MessageBuilder.AutoRegisterAll();
        }

        public async UniTask ConnectAsync(string ip, int port)
        {
            await Socket.ConnectAsync(ip, port);
        }

        public void Send(Message msg)
        {
            // 序列化逻辑可以放在这里或扩展方法中
            byte[] buffer = new byte[4 + msg.Length()];
            int index = 4;  // 写入长度占位 (暂空)
            msg.Writing(buffer, ref index);  // 写入消息
            
            // 回填长度 (总长度 - 4字节长度头)
            int len = index - 4;
            buffer[0] = (byte)(len & 0xFF);
            buffer[1] = (byte)((len >> 8) & 0xFF);
            buffer[2] = (byte)((len >> 16) & 0xFF);
            buffer[3] = (byte)((len >> 24) & 0xFF);

            Socket.SendAsync(buffer).Forget();
        }

        // 绑定业务逻辑
        public void Bind(object target) => Dispatcher.Bind(target);
        public void Unbind(object target) => Dispatcher.Unbind(target);

        public void Dispose()
        {
            Socket?.Dispose();
        }
    }
}