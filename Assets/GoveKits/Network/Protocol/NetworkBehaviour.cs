using UnityEngine;

namespace GoveKits.Network
{
    public class NetworkBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            if (NetManager.Instance != null)
                NetManager.Instance.Bind(this);
        }

        protected virtual void OnDisable()
        {
            if (NetManager.Instance != null)
                NetManager.Instance.Unbind(this);
        }
    }
}