
using UnityEngine;


namespace GoveKits.Runtime.Network.Protocol
{
    [RequireComponent(typeof(NetworkManager))]
    public class AutoConnection : MonoBehaviour
    {
        public string Host = "127.0.0.1";
        public int Port = 12345;


        public void Start()
        {
            NetworkManager.Instance.Client.Connect(Host, Port);
        }



        public void OnDestroy()
        {
            NetworkManager.Instance?.Client?.Disconnect();
        }
    }
}