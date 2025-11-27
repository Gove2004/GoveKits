


using System.Text;
using UnityEngine;

namespace GoveKits.Network
{
    // === 生成物体消息 ===
    [Message(Protocol.SpawnID)]
    public class SpawnMessage : Message
    {
        public string PrefabName;
        public int NetID;
        public int OwnerID;
        public Vector3 Pos;
        public Vector3 Rot;
        
        protected override int BodyLength() => 4 + Encoding.UTF8.GetByteCount(PrefabName) + 8 + 2 * 3 * 4;
        protected override void BodyWriting(byte[] b, ref int i)
        {
            WriteString(b, PrefabName, ref i);
            WriteInt(b, NetID, ref i);
            WriteInt(b, OwnerID, ref i);
            WriteVector3(b, Pos, ref i);
            WriteVector3(b, Rot, ref i);
        }
        protected override void BodyReading(byte[] b, ref int i)
        {
            PrefabName = ReadString(b, ref i);
            NetID = ReadInt(b, ref i);
            OwnerID = ReadInt(b, ref i);
            Pos = ReadVector3(b, ref i);
            Rot = ReadVector3(b, ref i);
        }
    }
}