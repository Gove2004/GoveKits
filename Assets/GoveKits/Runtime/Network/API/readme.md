```cs
// 1. 定义一个数据类
[System.Serializable]
public class UserInfo {
    public string name;
    public int age;
}

public async void Test()
{
    // --- POST: 自动序列化 ---
    var user = new UserInfo { name = "Gove", age = 18 };
    
    // WebAPI.Send(RequestData.Post(url, body))
    var res = await WebAPI.Send(RequestData.Post("/user/update", user));

    if (res.Success) {
        // 反序列化结果
        var serverResp = res.As<UserInfo>();
        Debug.Log("Updated: " + serverResp.name);
    }

    // --- GET: 带参数 ---
    var query = new Dictionary<string, string> { { "id", "1001" } };
    await WebAPI.Send(RequestData.Get("/user/detail", query));

    // --- PUT: 修改 ---
    await WebAPI.Send(RequestData.Put("/user/1001", user));

    // --- DELETE: 删除 ---
    await WebAPI.Send(RequestData.Delete("/user/1001"));
}
```