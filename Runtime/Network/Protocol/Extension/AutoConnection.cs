
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;


namespace GoveKits.Runtime.Network
{
    public class AutoConnection : MonoBehaviour
    {
        public string Host = "127.0.0.1";
        public int Port = 12345;
        

        public void Start()
        {
            ClientCore.ConnectAsync(Host, Port).Forget();
        }


        public void OnDestroy()
        {
            ClientCore.Shutdown();
        }
    }
}