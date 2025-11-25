
using UnityEngine;
using DG.Tweening;

namespace GoveKits.Network 
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkTransform : NetworkBehaviour   
    {
        [Header("Settings")]
        public float SyncRate = 0.1f; // 同步间隔 (10次/秒)
        public float MoveThreshold = 0.05f; // 移动阈值
        public float RotThreshold = 1f; // 旋转阈值

        private float _lastSyncTime;
        private Vector3 _lastPos;
        private Quaternion _lastRot;

        private void Start()
        {
            _lastPos = transform.position;
            _lastRot = transform.rotation;
        }

        private void Update()
        {
            // 只有拥有者才发送
            if (IsMine)
            {
                if (Time.time - _lastSyncTime > SyncRate)
                {
                    CheckAndSend();
                    _lastSyncTime = Time.time;
                }
            }
        }

        private void CheckAndSend()
        {
            bool hasMoved = Vector3.Distance(transform.position, _lastPos) > MoveThreshold;
            bool hasRotated = Quaternion.Angle(transform.rotation, _lastRot) > RotThreshold;

            if (hasMoved || hasRotated)
            {
                SendTransform();
                _lastPos = transform.position;
                _lastRot = transform.rotation;
            }
        }

        public void SendTransform()
        {
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
            // 过滤：只处理属于自己的 NetID
            if (msg.NetID != this.NetID) return;
            
            // 过滤：如果你是拥有者（例如本地预测移动），不要被服务器的回包拉回去
            // 除非你需要做强一致性的位置纠正
            if (IsMine && !NetworkManager.Instance.IsServer) return;

            // 使用 DOTween 平滑插值
            transform.DOKill();
            transform.DOMove(msg.position, SyncRate); // 时间设为 SyncRate 刚好衔接
            transform.DORotate(msg.rotation, SyncRate);
            transform.DOScale(msg.scale, SyncRate);
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