


using Cysharp.Threading.Tasks;
using GoveKits.Network.Examples;
using UnityEngine;

public class Ex : MonoBehaviour
{
    NetworkExample n = new NetworkExample();


    public void Start()
    {
        n.Initialize();
    }

    public void Update()
    {
        n.Update();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            n.SendPlayerMessage(42, "Hello World!").Forget();
        }
    }


    public void OnDestroy()
    {
        n.Shutdown();
    }
}