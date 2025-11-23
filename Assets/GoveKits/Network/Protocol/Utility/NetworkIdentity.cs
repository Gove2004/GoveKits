using UnityEngine;

namespace GoveKits.Network
{
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        public int NetID = 0;
        public int OwnerID = 0;
        
        // 判断是否是我的物体
        public bool IsMine => NetManager.Instance != null && NetManager.Instance.PlayerID == OwnerID;
    }
}