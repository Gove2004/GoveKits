using UnityEngine;
using DG.Tweening;
using GoveKits.Save;

namespace GoveKits.Network 
{
    /// <summary>
    /// 同步消息体基类，包含目标对象的网络ID
    /// </summary>
    public class SyncBody<T> : MessageBody where T : MessageBody, new()
    {
        public int NetID;  // 目标对象的网络ID
        public T SyncData;  // 具体的同步数据

        public SyncBody()
        {
            SyncData = new T();
        }

        public override int Length() => 4 + SyncData.Length(); // NetID(4) + 数据长度

        public override void Writing(byte[] buffer, ref int index)
        {
            WriteInt(buffer, NetID, ref index); // 先写 NetID
            SyncData.Writing(buffer, ref index);
        }

        public override void Reading(byte[] buffer, ref int index)
        {
            NetID = ReadInt(buffer, ref index); // 先读 NetID
            SyncData.Reading(buffer, ref index);
        }
    }





    public class NetworkTransform : NetworkBehaviour   
    {
        private void Update()
        {
            if (IsMine && Input.GetKeyDown(KeyCode.T))
            {
                SendTransform();
            }
        }

        public void SendTransform()
        {
            Debug.Log($"[NetworkTransform] Sending Transform {transform.position}, {transform.eulerAngles}, {transform.localScale}");
            var data = new TransformBody()
            {
                position = transform.position,
                rotation = transform.eulerAngles,
                scale = transform.localScale
            };
            var msg = new TransformMessage( data );
            SendSync(msg);
        }

        [MessageHandler(Protocol.TransformID)]
        public void OnReceiveTransform(TransformMessage msg)
        {
            Debug.Log($"[NetworkTransform] Received Transform for NetID {msg.Body.NetID}: {msg.Body.SyncData.position}, {msg.Body.SyncData.rotation}, {msg.Body.SyncData.scale}");
            if (msg.Body.NetID != this.NetID) return;
            transform.DOKill(); // 杀掉当前所有动画，防止冲突
            transform.DOMove(msg.Body.SyncData.position, 0.1f);
            transform.DORotate(msg.Body.SyncData.rotation, 0.1f);
            transform.DOScale(msg.Body.SyncData.scale, 0.1f);
        }
    }



    public class TransformBody : MessageBody
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        
        public override int Length() => 3 * 3 * 4; // 3个Vector3，每个Vector3有3个float，每个float占4字节
        public override void Reading(byte[] buffer, ref int index)
        {
            position = ReadVector3(buffer, ref index);
            rotation = ReadVector3(buffer, ref index);
            scale = ReadVector3(buffer, ref index);
        }
        public override void Writing(byte[] buffer, ref int index)
        {
            WriteVector3(buffer, position, ref index);
            WriteVector3(buffer, rotation, ref index);
            WriteVector3(buffer, scale, ref index);
        }
    }
    


    [Message(Protocol.TransformID)]
    public class TransformMessage : Message<SyncBody<TransformBody>>
    {
        public TransformMessage() : base() { }
        public TransformMessage(TransformBody bodyData)
        {
            Body.SyncData = bodyData;
        }
    }
}