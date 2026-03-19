using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Generated;
using GoveKits.Network;
using UnityEngine;

namespace GoveKits
{
    public class Test : MonoBehaviour
    {
        public string name = "Gove";
        public string pwd = "123";

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                _ = OnDo();
            }
        }


        private async UniTask OnDo()
        {
            var req = new LoginReq { Username = name, Password = pwd };

            Debug.Log($"开始登录...{req.Username}");

            // 【核心体验】await 等待结果！
            // 这里的 Call<LoginResp> 指定了我们期待的回包类型
            var resp = await RpcManager.Instance.Call<LoginResp>(req);

            if (resp != null && resp.Success)
            {
                Debug.Log($"登录成功 {resp.Message}");
                // LoadScene...
            }
            else
            {
                Debug.LogError("登录超时或失败");
            }
        }
    }
}
