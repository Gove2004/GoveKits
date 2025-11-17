using Cysharp.Threading.Tasks;
using UnityEngine;

using System.Text;


namespace GoveKits.Network
{
    public class NetSocketExample : MonoBehaviour
    {
        private NetSocket net;

        async void Start()
        {
            // 注册消息（按你当前实现，MessageBuilder 存储 prototype）
            MessageBuilder.AddMessage(1001, new PlayerMessage());

            // 连接（回调示例）
            net = new NetSocket("127.0.0.1", 12345,
                callbackSuccess: () => Debug.Log("Connected"),
                callbackFailure: () => Debug.LogWarning("Connect failed"));

            // 如果想在 Start 中等待首次连接完成可用 await/timeout 逻辑（当前构造是异步的回调风格）
            await UniTask.DelayFrame(1);
        }

        void Update()
        {
            // 每帧调度处理队列（不会阻塞主线程）
            // Process() 在现有实现中返回 UniTask，所以这里用 .Forget() 快速触发
            net?.Process().Forget();
        }

        // 一个示例发送函数（异步）
        public async UniTaskVoid SendPlayerInfo(int id, string name)
        {
            if (net == null) return;

            var msg = new PlayerMessage();
            msg.MsgData = new PlayerData { PlayerId = id, Name = name };
            // 发送并 await 完成（如果你不想 await，可使用 .Forget()）
            await net.Send(msg);
        }

        // UI 按钮示例调用（非 async）
        public void OnSendButtonClicked()
        {
            // 不等待发送完成
            SendPlayerInfo(42, "Alice").Forget();
        }

        private void OnDestroy()
        {
            net?.Close();
        }
    }
}





namespace GoveKits.Network
{
    public class PlayerData : MessageData
    {
        public int PlayerId;
        public string Name = "";

        public override int ByteLength()
        {
            return sizeof(int) + sizeof(int) + Encoding.UTF8.GetByteCount(Name);
        }

        public override byte[] Writing()
        {
            int idx = 0;
            byte[] bytes = new byte[ByteLength()];
            WriteInt(bytes, PlayerId, ref idx);
            WriteString(bytes, Name, ref idx);
            return bytes;
        }

        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            int idx = beginIndex;
            PlayerId = ReadInt(bytes, ref idx);
            Name = ReadString(bytes, ref idx);
            return idx - beginIndex;
        }
    }

    public class PlayerMessage : Message
    {
        public PlayerMessage()
        {
            MsgID = 1001;
            MsgData = new PlayerData();
        }

        private PlayerData Data => (PlayerData)MsgData;

        // 处理收到的消息（异步接口）
        public override UniTask Handle()
        {
            Debug.Log($"[PlayerMessage] Received PlayerId={Data.PlayerId}, Name={Data.Name}");
            // 举例：收到后发回一个确认（可选）
            return UniTask.CompletedTask;
        }
    }
}