这是一个为 **GoveKits.Network** 编写的详细 README 文档。

---

# GoveKits.Network

基于 **Unity** 和 **UniTask** 的轻量级、高性能网络框架。  
集成了 **HTTP 请求管理** 与 **TCP 长连接（多人联机）** 功能，专为中小规模游戏和即时应用设计。

## ✨ 核心特性

### 🌐 HTTP 模块 (NetAPI)
*   **全异步流**：基于 `UniTask`，告别协程回调地狱。
*   **智能队列**：支持并发限制（默认 5），防止瞬间请求过多卡死网络。
*   **自动重试**：网络波动或超时自动重试（仅限非 4xx 错误）。
*   **本地缓存**：支持 GET 请求的内存缓存，减少服务器压力。

### 🎮 Socket 模块 (NetworkManager)
*   **多种模式**：支持 **Client** (客户端)、**Server** (专用服务器) 和 **Host** (主机模式)。
*   **Host 优化**：Host 模式下，本地玩家与服务器通信走 **LocalConnection**（内存直接透传），零网络延迟，零序列化开销。
*   **消息分发**：基于反射和 Attribute 的自动消息路由，代码解耦。
*   **粘包处理**：内置 Length-Prefix 协议（4字节头），自动处理 TCP 粘包/半包。
*   **RPC 支持**：支持基础的远程过程调用（参数自动序列化）。

---

## 📦 安装与依赖

1.  **环境要求**：Unity 2020.3+
2.  **必要依赖**：
    *   [UniTask](https://github.com/Cysharp/UniTask) (必须安装，用于异步处理)
3.  **安装方式**：
    *   将 `GoveKits/Network` 文件夹拖入 Unity 项目 `Assets` 目录。

---

## 🚀 快速开始：HTTP 请求

使用 `NetAPI` 发起 HTTP 请求非常简单。不需要挂载组件，直接调用静态方法。

```csharp
using GoveKits.Network;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class HttpExample : MonoBehaviour
{
    private async void Start()
    {
        // 1. 配置请求数据
        var request = new RequestData
        {
            endpoint = "users/login",
            method = HttpMethod.POST,
            body = new { username = "gove", password = "123" }.ToString(), // 需自行处理 JSON 序列化
            useCache = false,
            retryCount = 3
        };

        // 2. 发起请求
        LogManager.Log("Requesting...");
        ResponseData response = await NetAPI.Request(request, this.GetCancellationTokenOnDestroy());

        // 3. 处理结果
        if (response.success)
        {
            LogManager.Log($"Success: {response.text}");
        }
        else
        {
            LogManager.LogError($"Failed: {response.error} (Code: {response.statusCode})");
        }
    }
}
```

---

## ⚔️ 快速开始：多人联机 (TCP)

### 1. 初始化网络管理器
在场景中创建一个 GameObject，挂载 `NetworkManager` 组件。
*   **Remote IP**: 服务器地址 (客户端用)
*   **Port**: 端口号
*   **Auto Connect**: 是否自动作为客户端连接

### 2. 定义消息协议
继承 `Message` 类，并使用 `[Message(id)]` 标记。

```csharp
using GoveKits.Network;

// 定义一个协议 ID (建议在 Protocol 类中统一管理)
public const int MSG_CHAT = 1001;

[Message(MSG_CHAT)]
public class ChatMessage : Message
{
    public string Content;
    public int ChannelID;

    // 返回消息体长度 (不含头)
    protected override int BodyLength()
    {
        // 字符串长度(4字节长度头 + 内容) + Int(4字节)
        return GetStringLength(Content) + 4; 
    }

    // 序列化
    protected override void BodyWriting(byte[] buffer, ref int index)
    {
        WriteString(buffer, Content, ref index);
        WriteInt(buffer, ChannelID, ref index);
    }

    // 反序列化
    protected override void BodyReading(byte[] buffer, ref int index)
    {
        Content = ReadString(buffer, ref index);
        ChannelID = ReadInt(buffer, ref index);
    }
}
```

### 3. 发送消息

```csharp
public void SendChat(string text)
{
    var msg = new ChatMessage 
    { 
        Content = text, 
        ChannelID = 1 
    };
    
    // 发送给服务器（如果是 Host/Server 则是广播）
    NetworkManager.Instance.Send(msg);
}
```

### 4. 接收消息 (自动路由)
在任何类中，只要绑定到 `NetworkManager`，即可通过 Attribute 处理消息。

```csharp
public class ChatSystem : MonoBehaviour
{
    void Start()
    {
        // 注册消息监听
        NetworkManager.Instance.Bind(this);
    }

    void OnDestroy()
    {
        // 记得解绑，防止内存泄漏
        NetworkManager.Instance.Unbind(this);
    }

    // 处理特定 ID 的消息
    [MessageHandler(MSG_CHAT)]
    private void OnReceiveChat(ChatMessage msg)
    {
        LogManager.Log($"收到玩家 {msg.Header.SenderID} 的消息: {msg.Content}");
    }
}
```

---

## 🛠️ 架构详解

### Host 模式原理
`NetworkManager` 采用了类似 UNet/Mirror 的 Host 架构：
*   **Server 模式**: 纯服务器，只处理 TCP 连接。
*   **Client 模式**: 纯客户端，通过 TCP 连接服务器。
*   **Host 模式**: 
    *   同时运行服务器逻辑和客户端逻辑。
    *   **LocalConnection**: Host 玩家的数据不走 TCP 协议栈，不经过序列化/反序列化，直接在内存中通过引用传递。
    *   其他玩家通过 TCP 连接进来。

### 消息结构
所有 TCP 数据包遵循以下格式：
`[总长度(4字节)] [消息ID(4字节)] [发送者ID(4字节)] [接收者ID(4字节)] [消息体(N字节)]`

---

## 📝 注意事项与优化建议

1.  **线程安全**: `NetAPI` 回调默认在主线程，但 `PacketParser` 的解析过程是在 `UniTask` 线程池中进行的，`MessageDispatcher` 已强制切换回主线程 (`SwitchToMainThread`)，因此业务逻辑是安全的。
2.  **TcpConnection 锁竞争**: 当前代码中发送缓冲区可能是静态共享的 (取决于具体实现)，在高并发场景下建议为每个连接分配独立的缓冲区。
3.  **RPC 字符串**: 目前 RPC 使用方法名字符串作为标识，建议在生产环境中改为 **Hash (int)** 以节省带宽。

