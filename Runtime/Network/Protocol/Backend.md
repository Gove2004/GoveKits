# GoveKits 网络通讯协议与后端接入指南

本文档旨在说明 GoveKits 客户端底层网络通信协议的设计规范。独立后端（Dedicated Server）只需严格遵循以下【封帧机制】和【序列化格式】，即可与 Unity/C# 客户端进行无缝的 TCP 通信。

## 1. 核心技术栈
* 传输层: TCP
* 序列化协议: MessagePack (极其紧凑的二进制序列化格式)
* 字节序 (Endianness): 小端序 (Little-Endian) （重要：包体长度头和消息ID均使用小端序编码）

---

## 2. 封帧协议 (Packet Framing)
由于 TCP 是流式传输协议，为了解决“粘包/半包”问题，客户端与服务端之间交互的所有数据必须严格按照以下 3 段式格式进行拼接：

[ Length (4 Bytes) ] + [ ProtocolID (2 Bytes) ] + [ Payload (N Bytes) ]

各字段严格定义如下：
1. Length (包长): 
   - 长度：4 字节
   - 类型：Int32 (Little-Endian)
   - 描述：整个数据帧的长度。
   - 【极其重要】：该长度的值 = ProtocolID的长度(2) + Payload的长度(N)。即不包含 Length 自身的 4 字节！

2. ProtocolID (消息路由ID): 
   - 长度：2 字节
   - 类型：UInt16 (Little-Endian)
   - 描述：消息类型对应的唯一业务 ID，用于路由分发。

3. Payload (真实数据体): 
   - 长度：N 字节
   - 类型：byte[]
   - 描述：使用 MessagePack 序列化后的纯粹业务数据对象。

封包示例：
假设你要发送一个长度为 10 字节的 Payload，其 ProtocolID 为 1001。
* Length = 2 + 10 = 12 (封包头前4个字节写入 0x0C 0x00 0x00 0x00)
* ProtocolID = 1001 (接下来2个字节写入 0xE9 0x03)
* Payload = 10 字节的 MessagePack 二进制数据
* 最终底层 Socket 一次性发送的总长度为：4 + 12 = 16 字节。

---

## 3. 序列化规范 (MessagePack)
GoveKits 客户端使用的是基于【整数键 (Integer Key)】的 MessagePack 序列化方案。
这意味着在 MessagePack 的底层表现形式中，对象被序列化为【数组 (Array / List)】而不是【字典 (Map / Dict)】。

【后端开发须知】：
当你在 Python/Go/Java 等语言中进行打包 (Pack) 时，请直接序列化为数组或列表，并严格保证元素的顺序与客户端 C# 类中定义的 [Key(x)] 顺序完全一致。
例如：C# 类包含 [Key(0)] int Id, [Key(1)] string Name。
在 Python 中打包时，应该打包列表：[100, "Alice"]，而不是字典 {"Id": 100, "Name": "Alice"}。

---

## 4. 后端实现参考 (Python 示例)

以下是一个使用 Python (socket, struct, msgpack) 实现的最简服务器 Demo，展示了如何正确地拆包、提取 ID 并反序列化：

```python
import socket
import struct
import msgpack # 需安装: pip install msgpack

def start_server(host='0.0.0.0', port=7777):
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind((host, port))
    server.listen(5)
    print(f"Server listening on {host}:{port}")

    while True:
        conn, addr = server.accept()
        print(f"New connection from {addr}")
        handle_client(conn)

def handle_client(conn):
    try:
        while True:
            # 1. 精确读取 4 字节的长度头
            length_data = recv_exact(conn, 4)
            if not length_data:
                break
            
            # '<i' 表示 Little-Endian, 32-bit integer
            frame_length = struct.unpack('<i', length_data)[0]
            
            # 2. 根据长度读取完整的数据帧 [ID + Payload]
            frame_data = recv_exact(conn, frame_length)
            if not frame_data:
                break

            # 3. 提取前 2 字节作为 ProtocolID ('<H' 表示 Little-Endian, 16-bit unsigned int)
            protocol_id = struct.unpack('<H', frame_data[:2])[0]
            
            # 4. 提取剩余字节作为 MessagePack Payload
            payload_data = frame_data[2:]
            
            # 5. 反序列化业务数据 (返回的是一个 List)
            msg_list = msgpack.unpackb(payload_data) if payload_data else []

            print(f"Received ProtocolID: {protocol_id}, Data: {msg_list}")

            # 业务处理示例...
            # if protocol_id == 1001: 
            #     reply_payload = msgpack.packb([200, "Success"]) # 序列化为 List
            #     send_message(conn, protocol_id=1002, payload=reply_payload)

    except Exception as e:
        print(f"Connection error: {e}")
    finally:
        conn.close()
        print("Connection closed.")

def send_message(conn, protocol_id, payload):
    # 计算 Frame Length = 2 bytes (ID) + Payload length
    frame_length = 2 + len(payload)
    
    # 按照 [Length(4)] + [ID(2)] 组装头部
    # '<iH' 表示 Little-Endian, 接着一个 Int32, 一个 UInt16
    header = struct.pack('<iH', frame_length, protocol_id)
    
    # 发送完整封包
    conn.sendall(header + payload)

def recv_exact(conn, num_bytes):
    """确保读取到足额的字节，解决 TCP 碎片化(半包)问题"""
    buffer = b''
    while len(buffer) < num_bytes:
        chunk = conn.recv(num_bytes - len(buffer))
        if not chunk:
            return None
        buffer += chunk
    return buffer

if __name__ == "__main__":
    start_server()
```