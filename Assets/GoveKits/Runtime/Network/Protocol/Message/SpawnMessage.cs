using UnityEngine;
using GoveKits.Binary;

namespace GoveKits.Network
{
    [Message(Protocol.SpawnID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class SpawnMessage : Message
    {
        [BinaryMember(10)] public string PrefabName;
        [BinaryMember(11)] public int NetID;
        [BinaryMember(12)] public int OwnerID;
        [BinaryMember(13)] public Vector3 Pos;
        [BinaryMember(14)] public Vector3 Rot; // 假设 Vector3 已在 Helper 中支持
    }
}