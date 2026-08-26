# Network 模块

GoveKits 网络子系统。提供 HTTP 请求、TCP/UDP 协议通信、帧同步/状态同步等网络基础设施。基于 MessagePack 序列化，支持客户端-服务端双向通信。

## 架构概览

```
GoveCore
├── HttpCore         → HTTP 请求（RESTful API，支持缓存、重试、并发节流）
├── ClientCore       → 客户端网络核心
└── ServerCore       → 服务端网络核心

ClientCore / ServerCore
├── ProtocolCenter   → 协议 ID 注册 + MessagePack 序列化/反序列化
├── MessageDispatcher → [MessageHandler] 反射分发
├── Session          → 连接包装，协议帧组装/剥离
└── IChannel         → 传输层抽象（TcpChannel / UdpChannel）

帧同步
├── ClientInputor    → 客户端输入收集器
├── ClientLerper     → 客户端状态插值器
├── ServerCollector  → 服务端输入收集器
└── ServerSimulator  → 服务端固定帧模拟器

ISyncable            → 实体同步接口（对接 SpawnCore）
```

## 初始化

```csharp
// GoveCore.Setup() 中会自动初始化 ClientCore 和 ServerCore

// 注册自定义协议消息（必须在发送消息前调用）
ClientCore.Protocol.ScanAndRegister();
ServerCore.Protocol.ScanAndRegister();

// 关闭
GoveCore.Close();
```

## HTTP 模块

RESTful HTTP 请求，支持缓存、重试、并发节流。

### 用法

```csharp
// GET + 缓存
var result = await HttpCore.Get("https://api.example.com/data")
    .EnableCache()
    .SendAsync();
var data = result.GetJson<MyData>(LogCore);

// POST
var response = await HttpCore.Post("https://api.example.com/login")
    .SetBody(new { username = "admin", password = "123" })
    .SetTimeout(30f)
    .SetRetry(2)
    .SendAsync();

// 注册后端
HttpCore.RegisterBackend(new UnityHttpBackend());
```

### HttpRequestBuilder 流式 API

```csharp
var builder = new HttpRequestBuilder(HttpMethod.Get, "https://api.example.com/users/123");
builder.SetHeader("Authorization", "Bearer xxx");
builder.SetQueryParam("fields", "name,email");
builder.SetTimeout(10f);
builder.SetRetry(3);
builder.EnableCache();

HttpResponse response = await builder.SendAsync();
```

### HttpCache

- TTL 300 秒，`ConcurrentDictionary` 线程安全
- 仅 GET + `EnableCache()` 的请求参与缓存
- `Close` 时自动清空

## 协议模块

基于 TCP 的二进制协议栈，使用 MessagePack 序列化。

### 协议帧格式

```
发送: [2 字节协议ID（小端）][N 字节 MessagePack 数据]
接收: DataSplitter 剥离 2 字节 ID → MessageDispatcher 按类型分发
```

TCP 传输层额外添加 4 字节长度前缀处理粘包/半包：
```
Socket 层: [4 字节长度][2 字节协议ID][N 字节 payload]
```

### 定义协议消息

```csharp
[ProtocolId(100)]
[MessagePackObject]
public class MoveRequest : IProtocolMessage
{
    [Key(0)] public Vector3 Position;
    [Key(1)] public long Timestamp;
}
```

### 注册消息处理器

```csharp
// 客户端签名: void OnMsg(TMsg)
public class MyClientProxy
{
    [MessageHandler]
    public void OnMove(MoveRequest msg)
    {
        // 处理移动请求
    }
}

// 服务端签名: void OnMsg(Session, TMsg)
public class MyServerProxy
{
    [MessageHandler]
    public void OnMove(Session session, MoveRequest msg)
    {
        // 处理客户端移动请求
    }
}

// 绑定
ClientCore.Dispatcher.Bind(myClientProxy);
ServerCore.Dispatcher.Bind(myServerProxy);
```

### 发送消息

```csharp
// 客户端发送
ClientCore.Send(new MoveRequest { Position = pos, Timestamp = ts });

// 服务端发送给指定客户端
ServerCore.SendTo(sessionId, new ServerNotify { Text = "Hello" });

// 服务端广播
ServerCore.Broadcast(new WorldUpdate { Tick = tick, Entities = states });
```

## 帧同步 / 状态同步

### 客户端帧同步

```csharp
// 启动帧同步
ClientCore.StartClient(submitInterval: 0.05f, serverTickRate: 0.05f);

// 每帧驱动
void Update()
{
    ClientCore.Update(Time.deltaTime);
}

// 修改输入数据
ClientCore.ModifyInput<InputData>(input =>
{
    input.Forward = Input.GetAxis("Vertical");
    input.Side = Input.GetAxis("Horizontal");
});
```

### 服务端帧同步

```csharp
// 启动服务端帧同步
ServerCore.StartServerSync(tickInterval: 0.05f);

// 每帧驱动
void Update()
{
    ServerCore.Update(Time.deltaTime);
}

// 监听逻辑 Tick
ServerCore.OnServerLogicTick += (inputs, tickInterval) =>
{
    // 执行游戏逻辑
    foreach (var kvp in inputs.Inputs)
    {
        var input = ProtocolCenter.Deserialize<InputData>(kvp.Value);
        // 处理玩家输入
    }
};
```

### ISyncable 接口

实现此接口的实体可自动参与帧同步的状态广播和客户端插值：

```csharp
public class PlayerEntity : ISpawnable, ISyncable
{
    public string SpawnKey => "player";
    public uint ObjectId { get; private set; }
    public uint NetId { get; set; }
    public bool IsDirty { get; set; }

    private Vector3 _position;
    private Vector3 _prevPosition;

    public (Type, byte[]) GetState()
    {
        _prevPosition = _position;
        IsDirty = true;
        var payload = ProtocolCenter.Serialize(_position);
        return (typeof(Vector3), payload);
    }

    public void ApplySnap(byte[] payload)
    {
        _prevPosition = _position = ProtocolCenter.Deserialize<Vector3>(payload);
    }

    public void ApplyLerp(byte[] fromPayload, byte[] toPayload, float t)
    {
        var from = ProtocolCenter.Deserialize<Vector3>(fromPayload);
        var to = ProtocolCenter.Deserialize<Vector3>(toPayload);
        _position = Vector3.Lerp(from, to, t);
    }
}
```

## 连接管理

### TcpChannel / UdpChannel

```csharp
// TCP 连接
var tcp = new TcpChannel();
await tcp.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));

// UDP 连接
var udp = new UdpChannel();
udp.Bind(0);
await udp.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
```

### DataSplitter

长度前缀拆包器，处理 TCP 粘包/半包：
- 协议格式：`[4 字节长度（小端）][N 字节 payload]`
- 最大包长 5MB
- 自动扩容缓冲区

## Session — 会话管理

```csharp
// 服务端获取会话
if (ServerCore.TryGetSession(sessionId, out var session))
{
    session.Send(new ChatMessage { Text = msg });
    session.Kick("违规操作");  // 踢出
    session.UserData = customData;  // 附加业务数据
}

// 监听关闭事件
session.OnClosed += (s, reason) => Debug.Log($"断开: {reason}");
```

## 注意事项

- **协议消息必须标注 `[ProtocolId]` 和 `[MessagePackObject]`**
- `ScanAndRegister()` 需在发送消息前调用，扫描已加载程序集中的所有协议类型
- `MessageDispatcher.Bind()` 后需通过 `Unbind()` 解绑，否则内存泄漏
- 帧同步的 `Update(deltaTime)` 需在每帧调用
- 服务端广播使用 `SendRaw` 优化（预序列化，避免重复）
- TCP 使用 4 字节长度前缀 + 2 字节协议 ID 双层帧格式
- UDP 无粘包问题，直接发送完整消息
- `ClientNetworkProxy` 和 `ServerNetworkProxy` 中的 `[MessageHandler]` 方法签名不同：客户端不带 Session 参数，服务端带 Session 参数
