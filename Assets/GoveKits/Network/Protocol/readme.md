协议是: [TotalLen 4][MsgID 4][Header 8][Body...]


这份代码构建了一套基于 **TCP + UniTask** 的轻量级 Unity 网络框架。它采用了 **消息号(MsgID)路由**、**二进制序列化**、**异步无阻塞** 的设计风格。

以下是该系统的使用说明书，包含 **基础概念**、**快速上手** 和 **完整示例（聊天功能）**。

---

# GoveKits.Network 网络系统使用指南

## 1. 系统架构简介

*   **通信层 (`ByteSocket`)**: 基于 `System.Net.Sockets` 和 `UniTask` 的异步 TCP 连接，处理粘包/半包（Length-Prefixed 格式）。
*   **协议层 (`Message`)**: 自定义二进制协议。结构为 `[Length(4)][MsgID(4)][Header(8)][Body(N)]`。
*   **路由层 (`MessageDispatcher`)**: 使用 C# 特性 (`Attribute`) 和反射，将特定的 MsgID 自动分发给对应的处理函数。
*   **业务层 (`NetworkBehaviour`)**: 类似 Unity 的 `MonoBehaviour`，但在 `OnEnable` 时自动注册消息监听，`OnDisable` 自动解绑。支持网络物体身份 (`NetworkIdentity`) 同步。

---

## 2. 核心依赖

使用此系统前，请确保项目中包含以下环境：
1.  **UniTask**: 用于异步处理 (Cysharp.Threading.Tasks)。
2.  **GoveKits.Save (BinaryData)**: 代码中继承的 `BinaryData` 类（你提供的代码中未包含具体实现，假设其提供了 `ReadInt`, `WriteInt`, `ReadString` 等基础序列化方法）。

---

## 3. 快速上手步骤

### 第一步：定义协议 ID
在 `Protocol.cs` 中添加你的消息 ID。

```csharp
public partial class Protocol
{
    // ... 现有协议
    public const int ChatMsgID = 1001; // 新增：聊天消息ID
}
```

### 第二步：定义消息体 (Body)
创建一个继承自 `MessageBody` 的类，负责具体的字段序列化。

```csharp
using GoveKits.Network;

public class ChatBody : MessageBody
{
    public string Content; // 聊天内容
    public string SenderName; // 发送者名字

    // 计算包体长度：根据你的 BinaryData 实现，字符串通常由 [长度(4) + 内容(N)] 组成
    // 这里假设 BinaryData.Length(string) 会返回正确长度
    public override int Length() 
    {
        // 假设 WriteString 写入：4字节长度 + 字符串字节
        return GetStringLength(Content) + GetStringLength(SenderName); 
    }

    public override void Writing(byte[] buffer, ref int index)
    {
        WriteString(buffer, SenderName, ref index);
        WriteString(buffer, Content, ref index);
    }

    public override void Reading(byte[] buffer, ref int index)
    {
        SenderName = ReadString(buffer, ref index);
        Content = ReadString(buffer, ref index);
    }
    
    // 辅助：如果你没有 GetStringLength，通常是 4 + Encoding.UTF8.GetByteCount(str)
    private int GetStringLength(string str) => 4 + System.Text.Encoding.UTF8.GetByteCount(str ?? "");
}
```

### 第三步：定义消息类 (Message)
创建一个继承自 `Message<T>` 的类，并绑定协议 ID。

```csharp
[Message(Protocol.ChatMsgID)] // ★ 必须标记 ID
public class ChatMessage : Message<ChatBody> 
{
    // 空构造函数用于反射创建
    public ChatMessage() { }

    // 便捷构造函数用于发送
    public ChatMessage(string name, string content)
    {
        Body.SenderName = name;
        Body.Content = content;
    }
}
```

### 第四步：发送消息
在任何地方调用 `NetManager` 发送。

```csharp
public void SendChat(string text)
{
    var msg = new ChatMessage("Player1", text);
    NetManager.Instance.Send(msg);
}
```

### 第五步：接收消息
在继承自 `NetworkBehaviour` 的脚本中编写处理函数。

```csharp
public class ChatSystem : NetworkBehaviour
{
    // ★ 核心：使用 Attribute 标记处理函数，参数类型必须对应
    [MessageHandler(Protocol.ChatMsgID)]
    private void OnReceiveChat(ChatMessage msg)
    {
        Debug.Log($"收到聊天: [{msg.Body.SenderName}]: {msg.Body.Content}");
        // 更新 UI ...
    }
}
```

---

## 4. 进阶：网络物体同步 (NetworkIdentity)

如果你需要同步场景中物体的位置、状态（例如类似 `NetworkTransform`），请遵循以下模式。

### 场景设置
1.  给需要同步的 GameObject 挂载 `NetworkIdentity` 组件。
2.  确保 `NetID` 在服务端和客户端唯一对应。
3.  挂载继承自 `NetworkBehaviour` 的逻辑脚本。

### 编写同步逻辑

**示例：同步玩家血量**

1.  **定义数据体**：
    ```csharp
    public class HealthBody : MessageBody
    {
        public int CurrentHP;
        public override int Length() => 4;
        public override void Writing(byte[] b, ref int i) => WriteInt(b, CurrentHP, ref i);
        public override void Reading(byte[] b, ref int i) => CurrentHP = ReadInt(b, ref i);
    }
    ```

2.  **定义消息（使用 SyncBody 包装）**：
    *   `SyncBody<T>` 会自动在你的数据前加一个 `NetID` 字段。
    ```csharp
    // 定义协议ID: Protocol.SyncHealthID = 2001;
    [Message(Protocol.SyncHealthID)]
    public class HealthMessage : Message<SyncBody<HealthBody>> { }
    ```

3.  **编写同步脚本**：
    ```csharp
    public class PlayerHealth : NetworkBehaviour
    {
        public int HP = 100;

        // 发送同步
        public void TakeDamage(int damage)
        {
            if (!IsMine) return; // 只有自己能修改并广播（或者由服务器广播）

            HP -= damage;

            // 创建消息
            var body = new HealthBody { CurrentHP = HP };
            var msg = new HealthMessage();
            msg.Body.SyncData = body; // 赋值数据
            
            // ★ 使用 SendSync，它会自动填入 this.NetID
            SendSync(msg); 
        }

        // 接收同步
        [MessageHandler(Protocol.SyncHealthID)]
        private void OnSyncHealth(HealthMessage msg)
        {
            // ★ 检查是否是发给这个物体的
            if (msg.Body.NetID != this.NetID) return;

            this.HP = msg.Body.SyncData.CurrentHP;
            Debug.Log($"物体 {this.NetID} 血量更新为: {HP}");
        }
    }
    ```

---

## 5. 生命周期与注意事项

### 启动流程
1.  场景中必须有一个 `NetManager` 单例。
2.  `NetManager` Awake 时会自动连接 `RemoteIP` 和 `RemotePort`。
3.  **关键**：服务器连接成功后，必须向客户端发送 `Protocol.PlayerInitID (1)` 消息，客户端收到后才会将 `IsLogged` 置为 `true`，从而允许发送其他业务消息。

### 线程安全
*   `MessageDispatcher` 在分发消息时使用了 `await UniTask.SwitchToMainThread()`，所以 **Handle 方法内是主线程安全的**，可以直接操作 UI 和 Unity 组件。

### 内存优化
*   `ByteSocket` 使用了 `64KB` 的接收缓冲区复用。
*   `PacketParser` 包含自动扩容和内存整理机制。
*   消息对象是通过 `new` 创建的，如果消息量极大，后续可考虑引入对象池 (`MessagePool`)。

### 常见错误排查
1.  **报错 "Has no [Message(id)] attribute"**: 忘记在 Message 类上加 `[Message(ID)]` 特性。
2.  **消息发不出去**:
    *   检查 Socket 是否连接 (`NetManager.Instance.IsConnected`)。
    *   检查是否已收到服务器的 Init 消息 (`NetManager.Instance.IsLogged`)。
3.  **接收不到消息**:
    *   检查脚本是否继承自 `NetworkBehaviour` 且处于 Active 状态。
    *   检查 `[MessageHandler(ID)]` 中的 ID 是否与 Message 类上的 ID 一致。
    *   如果是手动绑定的普通类，确保调用了 `NetManager.Instance.Bind(this)`。

---

## 6. 完整代码清单 (Example)

假设我们要实现一个简单的 "登录 + 聊天" demo。

```csharp
// 1. Protocol.cs
public partial class Protocol {
    public const int ChatID = 500;
}

// 2. Body & Message
public class ChatBody : MessageBody {
    public string Msg;
    public override int Length() => 4 + System.Text.Encoding.UTF8.GetByteCount(Msg ?? "");
    public override void Writing(byte[] b, ref int i) => WriteString(b, Msg, ref i);
    public override void Reading(byte[] b, ref int i) => Msg = ReadString(b, ref i);
}

[Message(Protocol.ChatID)]
public class ChatMsg : Message<ChatBody> {
    public ChatMsg() {}
    public ChatMsg(string text) { Body.Msg = text; }
}

// 3. UI 逻辑脚本
public class ChatUI : NetworkBehaviour 
{
    public InputField input;

    public void OnClickSend() 
    {
        if(string.IsNullOrEmpty(input.text)) return;
        
        // 发送
        NetManager.Instance.Send(new ChatMsg(input.text));
        input.text = "";
    }

    // 接收
    [MessageHandler(Protocol.ChatID)]
    private void OnRecvChat(ChatMsg msg)
    {
        Debug.Log($"服务器广播: {msg.Body.Msg}");
    }
}
```