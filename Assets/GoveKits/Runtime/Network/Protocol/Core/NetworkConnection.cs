using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkConnection
    {
        // 连接的远程端 ID（对面是谁）
        // 对于 Server 侧的连接，是玩家的 ID
        // 对于 Client 侧的连接，是 Server 的 ID (0)
        public int RemoteID { get; private set; }
        public bool IsAlive => _transport != null && _transport.IsConnected;

        private ITransport _transport;
        private readonly MessageDispatcher _dispatcher;
        private readonly PacketParser _parser;
        
        // 【关键新增】是否是服务端持有的连接？
        // true: 代表我是服务器，对面是玩家。我必须严格校验身份。
        // false: 代表我是玩家，对面是服务器。我信任服务器发来的 SenderID。
        private readonly bool _isServerSide; 

        public event Action OnDisconnected;

        public NetworkConnection(int remoteId, ITransport transport, MessageDispatcher dispatcher, bool isServerSide)
        {
            RemoteID = remoteId;
            _transport = transport;
            _dispatcher = dispatcher;
            _isServerSide = isServerSide; // 记录身份
            
            _parser = new PacketParser(OnMessageParsed);
            _transport.OnReceiveData = (data) => _parser.InputRawData(data, 0, data.Length);
            _transport.OnDisconnected = () => OnDisconnected?.Invoke();
        }

        // public void SetRemoteID(int id) => RemoteID = id;  // 暂时没有用处，先注释掉

        private async UniTask OnMessageParsed(Message msg)
        {
            // 【逻辑修正】
            if (_isServerSide)
            {
                // 如果我是服务器，必须强制修正发送者 ID，防止客户端伪造
                msg.Header.SenderID = RemoteID;
            }
            else
            {
                // 如果我是客户端，服务器发过来的包里写是谁发的，就是谁发的 (信任 Server 转发)
                // 不做任何修改，保留 msg.Header.SenderID
            }
            
            await _dispatcher.DispatchAsync(msg);
        }

        public void Send(Message msg)
        {
            if (!IsAlive) return;
            byte[] bytes = PacketParser.PackMessage(msg);
            _transport.Send(bytes);
        }

        public void Close()
        {
            _transport?.Close();
            _transport = null;
        }
    }
}