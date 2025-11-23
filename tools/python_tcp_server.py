#!/usr/bin/env python3
"""
Unity NetSession Server (Matched with GoveKits.Network)
-------------------------------------------------------
Packet Structure:
    [TotalLength (int32)] 
    [MsgID (int32)] 
    [SenderID (int32)] 
    [TargetID (int32)] 
    [Body (bytes...)]

Endian: Little-Endian (<)
"""

import asyncio
import struct
import argparse
import datetime
from typing import Dict

# ================= 配置区域 =================

# 协议常量 (必须与 C# BinaryData 读写一致)
ENDIAN_FMT = '<'       # 小端序
INT_SIZE = 4           # int32 字节数
HEADER_SIZE = 12       # MsgID(4) + SenderID(4) + TargetID(4)

# 消息 ID 定义 (需与 C# 保持一致)
# 建议在 C# 定义一个 SystemProtocol
MSG_PING               = 0     # 心跳包
MSG_SYS_Init           = 1  # 登录回包，告诉客户端它的 PlayerID
MSG_TRANSFORM          = 2  # 位置同步

# ===========================================

class Client:
    def __init__(self, writer: asyncio.StreamWriter, player_id: int):
        self.writer = writer
        self.player_id = player_id
        self.addr = writer.get_extra_info('peername')

    async def send_raw(self, data: bytes):
        """直接发送打包好的数据"""
        try:
            self.writer.write(data)
            await self.writer.drain()
        except Exception as e:
            print(f"Send error to {self.player_id}: {e}")

    async def send_message(self, msg_id: int, target_id: int, body: bytes):
        """服务器主动组包发送"""
        # 1. 构造 Header + Body
        # Header: MsgID, SenderID(Server=0), TargetID
        sender_id = 0 # 0 代表服务器
        header = struct.pack(f'{ENDIAN_FMT}iii', msg_id, sender_id, target_id)
        payload = header + body
        
        # 2. 构造 Length 前缀
        length = len(payload)
        packet = struct.pack(f'{ENDIAN_FMT}i', length) + payload
        
        await self.send_raw(packet)

# 全局状态
clients: Dict[asyncio.StreamWriter, Client] = {}
next_player_id = 1000 

def log(info):
    print(f"[{datetime.datetime.now().strftime('%H:%M:%S')}] {info}")

async def broadcast(sender_client: Client, payload: bytes, include_self=False):
    """
    广播转发
    Payload 必须是已经包含 Header 的完整数据块 (不包含 Length 前缀)
    """
    # 1. 预先打包好带 Length 的完整包，避免对每个客户端重复打包
    length = len(payload)
    packet = struct.pack(f'{ENDIAN_FMT}i', length) + payload

    for writer, client in clients.items():
        if not include_self and client == sender_client:
            continue
        await client.send_raw(packet)

async def handle_client(reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
    global next_player_id
    
    # 1. 连接初始化
    player_id = next_player_id
    next_player_id += 1
    client = Client(writer, player_id)
    clients[writer] = client
    
    log(f"Client Connected: {client.addr} | Assigned ID: {player_id}")

    # 2. 【关键】发送登录确认包 (告诉客户端它是谁)
    # 对应 C# LoginMsg 的 Body 结构 (假设 LoginBody 只有一个 int UserId)
    # C# 如果是: public class LoginBody { public int UserId; }
    login_body = struct.pack(f'{ENDIAN_FMT}i', player_id)
    await client.send_message(MSG_SYS_Init, player_id, login_body)

    try:
        while True:
            # Step 1: 读取 Length (4字节)
            length_data = await reader.readexactly(INT_SIZE)
            if not length_data:
                break
            
            (body_len,) = struct.unpack(f'{ENDIAN_FMT}i', length_data)

            # Step 2: 读取 Payload
            payload = await reader.readexactly(body_len)

            if len(payload) < HEADER_SIZE:
                log(f"Error: Packet too short from {player_id}")
                continue

            # Step 3: 解析 Header
            # C# Message Writing 顺序: MsgID -> SenderID -> TargetID
            header_data = payload[:HEADER_SIZE]
            body_data = payload[HEADER_SIZE:]
            
            msg_id, _, target_id = struct.unpack(f'{ENDIAN_FMT}iii', header_data)

            # Step 4: 【安全覆写】
            # 无论 C# 发来什么 SenderID，强制改成服务器分配的 player_id
            # 这样接收方能确信消息来源是真实的
            new_header = struct.pack(f'{ENDIAN_FMT}iii', msg_id, player_id, target_id)
            
            # 组合成待转发的 Payload (Header + Body)
            forward_payload = new_header + body_data

            # Step 5: 业务分发
            if msg_id == MSG_PING:
                # 默认转发
                log(f"Relay Msg {msg_id}: {player_id} -> All")
                await broadcast(client, forward_payload, include_self=True)
 
            elif msg_id == MSG_TRANSFORM:
                # 位置同步：转发给除自己以外的所有人
                log(f"Relay Msg {msg_id}: {player_id} -> {target_id}")
                # 看数据
                net_id, pos_x, pos_y, pos_z, rot_x, rot_y, rot_z, sca_x, sca_y, sca_z = struct.unpack(f'{ENDIAN_FMT}ifffffffff', body_data)
                log(f"  Pos: ({pos_x:.2f}, {pos_y:.2f}, {pos_z:.2f})")
                log(f"  Rot: ({rot_x:.2f}, {rot_y:.2f}, {rot_z:.2f})")
                log(f"  Sca: ({sca_x:.2f}, {sca_y:.2f}, {sca_z:.2f})")
                await broadcast(client, forward_payload, include_self=True)
                
            else:
                # 默认转发
                log(f"Relay Msg {msg_id}: {player_id} -> All")
                await broadcast(client, forward_payload, include_self=True)

    except asyncio.IncompleteReadError:
        pass 
    except ConnectionResetError:
        pass
    except Exception as e:
        log(f"Error with {player_id}: {e}")
    finally:
        log(f"Client Disconnected: {player_id}")
        if writer in clients:
            del clients[writer]
        writer.close()
        await writer.wait_closed()

async def main(host, port):
    server = await asyncio.start_server(handle_client, host, port)
    log(f'Server running on {host}:{port}')
    async with server:
        await server.serve_forever()

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('host', nargs='?', default='0.0.0.0')
    parser.add_argument('port', nargs='?', default=12345, type=int)
    args = parser.parse_args()
    
    try:
        asyncio.run(main(args.host, args.port))
    except KeyboardInterrupt:
        print("\nServer stopped.")