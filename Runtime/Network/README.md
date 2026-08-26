# Network 模块

GoveKits 网络子系统（v3.0.0 起不再自研协议栈）。网络通信由内置 **Mirror** 负责，HTTP 请求基于 **UnityWebRequest** 封装。

## 架构概览

```
Network/
├── Http/            UnityWebRequest + UniTask 异步封装（HttpCore）
└── Mirror/          Mirror 接入层（GoveKits.Mirror 程序集，NetworkCore 门面）
```

- 内置 Mirror 96.11.0（MIT），见 `Plugins/Mirror/`，**使用方勿重复安装**
- 协议、帧同步、状态同步、MessagePack 序列化等自研实现已全部移除

## HTTP 模块（HttpCore）

基于 UnityWebRequest，UniTask 异步，统一返回 `HttpResponse`。

### 用法

```csharp
// GET
HttpResponse resp = await HttpCore.GetAsync("https://api.example.com/data");
if (resp.IsSuccess)
    Debug.Log(resp.Text);

// GET + 自动反序列化 JSON
var data = await HttpCore.GetJsonAsync<MyData>("https://api.example.com/data");

// POST JSON
HttpResponse resp = await HttpCore.PostJsonAsync("https://api.example.com/login",
    new { username = "admin", password = "123" });

// POST 原始字节
byte[] body = Encoding.UTF8.GetBytes("...");
HttpResponse resp = await HttpCore.PostAsync("https://api.example.com/upload", body, "application/octet-stream");

// 自定义请求头 / 超时
var resp = await HttpCore.GetAsync(url,
    headers: new Dictionary<string, string> { ["Authorization"] = "Bearer xxx" },
    timeout: 10f);
```

### API 一览

| 方法 | 说明 |
|------|------|
| `GetAsync(url, headers, timeout)` | GET 请求 |
| `PostAsync(url, body, contentType, headers, timeout)` | POST 原始字节 |
| `PostJsonAsync(url, json/object, ...)` | POST JSON（支持对象自动序列化） |
| `PutAsync(url, body, contentType, ...)` | PUT 请求 |
| `DeleteAsync(url, headers, timeout)` | DELETE 请求 |
| `GetJsonAsync<T>(url, ...)` | GET + JSON 反序列化（失败返回 default 并记日志） |

- 默认超时 30s，最大并发 8（超出排队）
- `HttpResponse` 字段：`IsSuccess` / `StatusCode` / `ErrorMsg` / `Text`

## Mirror 接入层（NetworkCore）

```csharp
using GoveKits.Runtime.Network;

bool isServer = NetworkCore.IsServer;   // NetworkServer.active
bool isClient = NetworkCore.IsClient;   // NetworkClient.active
bool isHost   = NetworkCore.IsHost;     // 服务端 + 客户端一体
```

业务接入请直接使用 Mirror 原生 API（`NetworkManager` / `NetworkBehaviour` / `SyncVar` 等），
GoveKits 不做二次封装。`NetworkCore` 仅提供运行状态查询与后续桥接扩展点。
