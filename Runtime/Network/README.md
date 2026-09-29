# Runtime/Network —— 网络模块

UnityWebRequest 封装的 HTTP 客户端 + 内置 Mirror 网络（多人联机），以静态门面统一入口。

## 模块架构

```
Runtime/Network/
├── Http/               HTTP 客户端 —— UnityWebRequest + UniTask 封装，统一返回 HttpResponse
└── Mirror/             Mirror 接入层 —— NetworkCore 门面暴露运行状态（内置 Mirror 库）
```

## Http —— HTTP 客户端

所有方法异步返回 `HttpResponse`，支持 GET/POST/PUT/DELETE、JSON 序列化、自定义头与超时。

```csharp
// 1. GET
HttpResponse resp = await HttpCore.GetAsync("https://api.example.com/items");
if (resp.IsSuccess)
{
    string json = resp.Text;                             // 响应体文本
    long code = resp.StatusCode;                         // 状态码
}
else
{
    LogCore.Error("Http", $"请求失败: {resp.ErrorMsg}");   // 失败信息
}

// 2. GET + 反序列化（GetJsonAsync 直接返回对象）
var items = await HttpCore.GetJsonAsync<List<ItemDto>>("https://api.example.com/items");

// 3. POST —— 对象自动转 JSON
var resp2 = await HttpCore.PostJsonAsync("https://api.example.com/login", new
{
    username = "admin",
    password = "123456"
});

// 4. POST —— 原始字节体 / 指定 Content-Type
byte[] data = Encoding.UTF8.GetBytes("hello");
var resp3 = await HttpCore.PostAsync("https://api.example.com/upload", data, "text/plain");

// 5. PUT / DELETE
var resp4 = await HttpCore.PutAsync("https://api.example.com/items/1", bytes);
var resp5 = await HttpCore.DeleteAsync("https://api.example.com/items/1");

// 6. 自定义头 / 超时
var headers = new Dictionary<string, string> { { "Authorization", "Bearer xxx" } };
var resp6 = await HttpCore.GetAsync("https://api.example.com/me", headers, timeout: 10f);

// 7. 取消请求（所有方法最后一个参数都是 CancellationToken，取消时返回失败响应）
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var resp7 = await HttpCore.GetAsync("https://api.example.com/slow", cancellationToken: cts.Token);
```

### HttpResponse

| 字段 | 说明 |
|---|---|
| `IsSuccess` | 是否成功（2xx 为 true） |
| `StatusCode` | HTTP 状态码 |
| `ErrorMsg` | 失败时的错误描述（成功为 null） |
| `Text` | 响应体文本 |

## Mirror —— 多人联机

内置 Mirror 网络库（随包分发，勿重复安装），`NetworkCore` 门面统一暴露运行状态，业务代码不直接散落 Mirror API。

```csharp
// 运行状态查询
bool isServer = NetworkCore.IsServer;    // 服务端（含 Host）
bool isClient = NetworkCore.IsClient;    // 客户端（含 Host）
bool isHost   = NetworkCore.IsHost;      // Host（服务端+客户端一体）
```

Mirror 的完整开发模式（NetworkManager、NetworkBehaviour、SyncVar/Command/Rpc、NetworkTransform 等）遵循 Mirror 官方用法，接入层通过 `GoveKits.Mirror` 程序集引用。典型骨架：

```csharp
// 场景挂 Mirror 的 NetworkManager（设置传输、注册预制体）
// 网络对象脚本继承 NetworkBehaviour，用 [SyncVar]/[Command]/[ClientRpc] 同步
public class Player : NetworkBehaviour
{
    [SyncVar] public int Hp;

    [Command]
    public void CmdTakeDamage(int dmg) { Hp -= dmg; }

    [ClientRpc]
    public void RpcPlayHitFx() { /* 播放受击特效 */ }
}
```
