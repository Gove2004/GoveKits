
---

# GoveKits.Network 使用说明书

**基于**：Unity + UniTask + TCP Socket  
**特点**：全异步、零GC序列化、注解驱动、自动分包/粘包处理、大小端统一（Little-Endian）。

---

## 1. 环境依赖

在开始之前，请确保项目已安装以下依赖：
*   **UniTask** (Cysharp.Threading.Tasks)：用于异步处理。
*   **GoveKits.Network 源码**：包含 `NetSession`, `NetSocket`, `Message`, `BinaryData` 等核心脚本。

---

## 2. 快速开始 (Quick Start)

### 第一步：场景配置
1.  在场景中创建一个空的 GameObject，命名为 `[Network]`。
2.  挂载 **`NetSession`** 脚本。
    *   设置 `Remote IP` 和 `Remote Port`。
3.  挂载 **`Heartbeat`** 脚本。
    *   设置心跳间隔（例如 5秒）。



---

## 3. 详细开发流程

### 3.1 定义通信协议 (Protocol)

假设我们要定义一个 **登录消息** (ID: 1001)，包含用户名和密码。

**规则**：
*   所有业务数据类继承 `BinaryData`。
*   所有消息类继承 `Message<T>`。
*   使用 `[NetMessage(id)]` 标记消息 ID。
*   **注意**：所有读写顺序必须一致！

```csharp
using GoveKits.Network;

// 1. 定义数据体 (Body)
public class LoginData : BinaryData
{
    public string Username;
    public string Password;

    // 计算包体长度：固定长度 + 字符串长度(int长度头+utf8字节)
    public override int Length()
    {
        return (4 + System.Text.Encoding.UTF8.GetByteCount(Username)) +
               (4 + System.Text.Encoding.UTF8.GetByteCount(Password));
    }

    // 序列化 (Write)
    public override void Writing(byte[] buffer, ref int index)
    {
        WriteString(buffer, Username, ref index);
        WriteString(buffer, Password, ref index);
    }

    // 反序列化 (Read)
    public override void Reading(byte[] buffer, ref int index)
    {
        Username = ReadString(buffer, ref index);
        Password = ReadString(buffer, ref index);
    }
}

// 2. 定义消息外壳 (Message)
[NetMessage(1001)] // <--- 自动注册 ID
public class MsgLogin : Message<LoginData>
{
    // 空构造函数是必须的
    public MsgLogin() { }
}
```

### 3.2 发送消息 (Send)

在任意逻辑中调用 `NetSession.Instance.Send`：

```csharp
public void SendLoginRequest()
{
    var msg = new MsgLogin();
    msg.MsgData.Username = "Gove";
    msg.MsgData.Password = "123456";

    NetSession.Instance.Send(msg);
    Debug.Log("登录请求已发送");
}
```

### 3.3 接收与处理消息 (Handle)

#### 方式一：使用 Lambda 表达式（推荐，简洁）

```csharp
void Start()
{
    // 注册监听
    NetSession.Instance.Register(1001, new MessageHandler<MsgLogin>(OnLoginResponse));
}

// 回调函数
private void OnLoginResponse(MsgLogin msg)
{
    Debug.Log($"收到登录回包，用户名: {msg.MsgData.Username}");
}
```

#### 方式二：继承 Handler 类（适合复杂逻辑）

```csharp
public class LoginHandler : MessageHandler<MsgLogin>
{
    // 构造函数传入处理逻辑，或者直接重写 Run
    public LoginHandler(Action<MsgLogin> action) : base(action) { }
}
// 使用：NetSession.Instance.Register(1001, new LoginHandler(OnLoginResponse));
```

### 3.4 注销监听 (Unregister)

**重要**：在组件销毁时（`OnDestroy`），务必注销监听，否则会导致内存泄漏或报错。

```csharp
private IMessageHandler _loginHandler;

void Start()
{
    _loginHandler = NetSession.Instance.Register(1001, new MessageHandler<MsgLogin>(OnMsg));
}

void OnDestroy()
{
    if (NetSession.Instance != null)
    {
        NetSession.Instance.Unregister(1001, _loginHandler);
    }
}
```

---

## 4. 心跳机制 (Heartbeat)

框架内置了心跳保活机制。

1.  **定义心跳包**：
    ```csharp
    // 定义空数据
    public class EmptyData : BinaryData {
        public override int Length() => 0;
        public override void Writing(byte[] buffer, ref int index) { }
        public override void Reading(byte[] buffer, ref int index) { }
    }

    [NetMessage(1)] // 假设心跳ID为1
    public class MsgHeartbeat : Message<EmptyData> { }
    ```
2.  **配置脚本**：
    确保 `Heartbeat.cs` 脚本挂载在场景中，它会自动每隔 `Interval` 秒发送 `MsgHeartbeat`。
3.  **超时断开**：
    `Heartbeat.cs` 包含超时检测，若超过 `Timeout` 秒未收到服务端回复，将自动断开连接。

---

## 5. 服务端对接标准 (Protocol Spec)

请将此标准发给后端开发人员（Python/Go/C++/Java）。

*   **字节序 (Endianness)**：**Little-Endian (小端序)** `<`
*   **通信协议头 (Header)**：共 8 字节
    *   `[0-3]` 字节：**Message ID** (int32)
    *   `[4-7]` 字节：**Body Length** (int32)
*   **包体 (Body)**：
    *   紧跟 Header 之后，长度为 `Body Length`。
    *   字符串格式：`Length (int32)` + `UTF-8 Bytes`。

**Python 服务端示例片段**：
```python
# 必须使用 '<' (小端)
HEADER_FMT = '<ii' 

# 解析头
msg_id, body_len = struct.unpack(HEADER_FMT, header_bytes)

# 解析字符串 (先读4字节长度，再读内容)
str_len = struct.unpack('<i', data[0:4])[0]
str_val = data[4:4+str_len].decode('utf-8')
```

---

## 6. 常见问题排查 (FAQ)

**Q: 报错 `MsgID 16777216 not registered`？**

A: 大小端不匹配。服务端发的是小端 `1` (`01 00 00 00`)，客户端按大端解析成了 `16777216`。请确保 `BinaryData`, `PacketParser`, `NetSession` 中均使用了 **移位操作的小端逻辑**（详见代码修正部分）。

**Q: 发送消息报错 `NullReferenceException`？**

A: 检查该消息类是否添加了 `[NetMessage(id)]` 特性，并确保在 `Awake` 中调用了 `MessageBuilder.AutoRegisterAll()`。

**Q: 切换场景后报错 `MissingReferenceException`？**

A: 你在旧场景的 `MonoBehaviour` 里注册了消息监听，但场景销毁时没调用 `Unregister`。当网络消息回来时，回调试图访问已销毁的对象。

**Q: 如何修改缓存区大小？**

A: 修改 `NetSocket.cs` 中的 `ReceiveBufferSize` (默认 64KB) 和 `PacketParser.cs` 构造函数中的默认容量。

---

祝开发顺利！ 🚀