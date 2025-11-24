using UnityEngine;
using DG.Tweening;
using GoveKits.Save;

namespace GoveKits.Network 
{
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
            var msg = new TransformSyncMessage()
            {
                position = transform.position,
                rotation = transform.eulerAngles,
                scale = transform.localScale
            };
            SendSync(msg);
        }

        [MessageHandler(Protocol.TransformSyncID)]
        public void OnReceiveTransform(TransformSyncMessage msg)
        {
            Debug.Log($"[NetworkTransform] Received Transform for NetID {msg.NetID}: {msg.position}, {msg.rotation}, {msg.scale}");
            if (msg.NetID != this.NetID) return;
            transform.DOKill(); // 杀掉当前所有动画，防止冲突
            transform.DOMove(msg.position, 0.1f);
            transform.DORotate(msg.rotation, 0.1f);
            transform.DOScale(msg.scale, 0.1f);
        }
    }


    [Message(Protocol.TransformSyncID)]
    public class TransformSyncMessage : SyncMessage
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        
        protected override int SyncDataLength() => 3 * 3 * 4; // 3个Vector3，每个Vector3有3个float，每个float占4字节
        protected override void SyncDataReading(byte[] buffer, ref int index)
        {
            position = ReadVector3(buffer, ref index);
            rotation = ReadVector3(buffer, ref index);
            scale = ReadVector3(buffer, ref index);
        }
        protected override void SyncDataWriting(byte[] buffer, ref int index)
        {
            WriteVector3(buffer, position, ref index);
            WriteVector3(buffer, rotation, ref index);
            WriteVector3(buffer, scale, ref index);
        }
    }
}