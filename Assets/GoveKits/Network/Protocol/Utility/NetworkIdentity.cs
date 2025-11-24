using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkIdentity : MonoBehaviour
    {
        public string PrefabName = "";
        public int NetID = 0;
        public int OwnerID = 0;

        public bool IsMine
        {
            get
            {
                if (NetworkManager.Instance == null) return false;
                // 如果是场景物体(OwnerID=0)且我是Host，我有权限
                if (OwnerID == 0 && NetworkManager.Instance.IsHost) return true;
                return NetworkManager.Instance.MyPlayerID == OwnerID;
            }
        }
    }
}