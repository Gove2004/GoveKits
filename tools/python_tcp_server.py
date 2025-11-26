#!/usr/bin/env python3
import asyncio
import struct
import argparse
import logging
from datetime import datetime
from typing import Dict, Optional

# ================= 配置与协议定义 =================

# 日志配置
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s [%(levelname)s] %(message)s',
    datefmt='%H:%M:%S'
)
logger = logging.getLogger("NetServer")

# 基础类型大小
INT_SIZE = 4
HEADER_SIZE = 12  # MsgID(4) + SenderID(4) + TargetID(4)
ENDIAN = '<'      # Little-Endian

# --- Protocol ID (需与 C# GoveKits.Network.Protocol 保持一致) ---
MSG_HELLO           = 1  # ID 分配
MSG_PING_PONG       = 0  # 心跳
MSG_TRANSFORM       = 2   # 位置同步 (示例)
MSG_RPC             = 5  # RPC 调用


# --- Target ID 定义 ---
ID_SERVER           = 0
ID_BROADCAST        = -1
ID_CLIENT_TEMP      = -1

# ================= 工具类 =================

class BinaryReader:
    """辅助解析 C# BinaryData 格式的数据"""
    def __init__(self, data: bytes):
        self.data = data
        self.offset = 0

    def read_int(self) -> int:
        val = struct.unpack_from(f'{ENDIAN}i', self.data, self.offset)[0]
        self.offset += 4
        return val

    def read_float(self) -> float:
        val = struct.unpack_from(f'{ENDIAN}f', self.data, self.offset)[0]
        self.offset += 4
        return val
    
    def read_byte(self) -> int:
        val = struct.unpack_from(f'{ENDIAN}B', self.data, self.offset)[0]
        self.offset += 1
        return val

    def read_string(self) -> str:
        # C# WriteString: 先写 int 长度，再写 bytes
        length = self.read_int()
        if length == 0:
            return ""
        str_bytes = self.data[self.offset : self.offset + length]
        self.offset += length
        return str_bytes.decode('utf-8')

# ================= 核心类 =================

class ClientSession:
    def __init__(self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter, player_id: int, server):
        self.reader = reader
        self.writer = writer
        self.player_id = player_id
        self.server = server
        self.addr = writer.get_extra_info('peername')
        self.is_alive = True

    async def send_packet(self, msg_id: int, target_id: int, body: bytes):
        """服务器主动发送消息"""
        try:
            # 1. 构造 Header (SenderID 固定填 0/Server)
            header = struct.pack(f'{ENDIAN}iii', msg_id, ID_SERVER, target_id)
            payload = header + body
            
            # 2. 构造 Length + Payload
            length = len(payload)
            packet = struct.pack(f'{ENDIAN}i', length) + payload
            
            self.writer.write(packet)
            await self.writer.drain()
        except Exception as e:
            logger.error(f"Send Error to {self.player_id}: {e}")
            await self.disconnect()

    async def send_raw(self, packet_data: bytes):
        """发送预组装好的原始数据包 (用于广播转发)"""
        try:
            self.writer.write(packet_data)
            await self.writer.drain()
        except Exception as e:
            logger.error(f"Raw Send Error to {self.player_id}: {e}")
            await self.disconnect()

    async def disconnect(self):
        if not self.is_alive: return
        self.is_alive = False
        self.server.remove_client(self.player_id)
        try:
            self.writer.close()
            await self.writer.wait_closed()
        except:
            pass
        logger.info(f"Client Disconnected: {self.player_id} ({self.addr})")

    async def listen_loop(self):
        try:
            while self.is_alive:
                # 1. 读取包头长度 (int32)
                length_bytes = await self.reader.readexactly(INT_SIZE)
                (body_len,) = struct.unpack(f'{ENDIAN}i', length_bytes)

                # 2. 读取包体 (MsgID + Header + Body)
                payload = await self.reader.readexactly(body_len)
                
                if len(payload) < HEADER_SIZE:
                    continue

                # 3. 处理逻辑
                await self.server.handle_packet(self, payload)

        except (asyncio.IncompleteReadError, ConnectionResetError):
            pass  # 正常断开
        except Exception as e:
            logger.error(f"Read Loop Error {self.player_id}: {e}")
        finally:
            await self.disconnect()


class GameServer:
    def __init__(self):
        self.clients: Dict[int, ClientSession] = {}
        self.next_id = 100
        
    def remove_client(self, pid):
        if pid in self.clients:
            del self.clients[pid]

    async def handle_connection(self, reader, writer):
        # 1. 分配 ID
        pid = self.next_id
        self.next_id += 1
        
        session = ClientSession(reader, writer, pid, self)
        self.clients[pid] = session
        logger.info(f"Client Connected: {session.addr} -> Assigned ID: {pid}")

        # 2. 发送 HelloMessage (握手)
        # Body: [PlayerID (int)]
        body = struct.pack(f'{ENDIAN}i', pid)
        await session.send_packet(MSG_HELLO, pid, body)

        # 3. 启动接收循环
        await session.listen_loop()

    async def handle_packet(self, sender: ClientSession, payload: bytes):
        """核心路由与处理逻辑"""
        # 解析 Header
        header_data = payload[:HEADER_SIZE]
        body_data = payload[HEADER_SIZE:]
        
        msg_id, _, target_id = struct.unpack(f'{ENDIAN}iii', header_data)

        # --- 安全机制：强制覆写 SenderID ---
        # 防止客户端伪造 ID，转发时必须由服务器盖戳
        new_header = struct.pack(f'{ENDIAN}iii', msg_id, sender.player_id, target_id)
        
        # 预打包转发数据 (Length + NewHeader + Body)
        # 这样广播时不需要为每个客户端重复打包
        forward_payload = new_header + body_data
        forward_packet = struct.pack(f'{ENDIAN}i', len(forward_payload)) + forward_payload

        # --- 路由逻辑 ---
        
        # Case A: 发送给服务器处理 (Target == 0)
        if target_id == ID_SERVER:
            await self.process_server_logic(sender, msg_id, body_data, forward_packet)
        
        # Case B: 广播 (Target == -1)
        elif target_id == ID_BROADCAST:
            await self.broadcast(forward_packet, exclude_id=sender.player_id)
            
        # Case C: 单发给特定玩家
        else:
            target_client = self.clients.get(target_id)
            if target_client:
                await target_client.send_raw(forward_packet)
            else:
                # logger.warning(f"Target {target_id} not found from {sender.player_id}")
                pass

    async def process_server_logic(self, sender: ClientSession, msg_id: int, body: bytes, echo_packet: bytes):
        """处理发给服务器的业务逻辑"""
        
        # 1. 心跳包 PingPong
        if msg_id == MSG_PING_PONG:
            # Server 收到 Ping，回复 Pong (原样发回)
            # 这里的 echo_packet 已经包含修正后的 sender_id，可以直接发回给 target(即 sender)
            # 但我们需要修改 TargetID 为 sender.player_id 才能发回去吗？
            # 不，send_raw 是底层发送，直接把 bytes 给 socket。
            # C# 客户端收到包时，Header.TargetID 是多少不重要(或者是0)，重要的是 SenderID 是谁。
            # 为了严谨，我们应该重新打包一个 TargetID 指向发送者的包。
            
            # 简单做法：C# PingPong 逻辑里 SendToPlayer 会自动改 TargetID
            # Python 里我们手动回包
            await sender.send_packet(MSG_PING_PONG, sender.player_id, body)
            logger.debug(f"Ping from {sender.player_id}")

        # 2. RPC
        elif msg_id == MSG_RPC:
            # 解析 RPC 内容进行调试
            try:
                reader = BinaryReader(body)
                net_id = reader.read_int()
                method_name = reader.read_string()
                arg_count = reader.read_byte()
                
                logger.info(f"[RPC] From {sender.player_id} | Method: {method_name} | NetID: {net_id} | Args: {arg_count}")
                
                # 如果是 Server RPC，这里执行逻辑...
                
                # 默认行为：如果是发给 Server 的 RPC，通常意味着 Server 要权威执行并广播
                # 这里简单实现为：广播给其他人 (实现 RpcTarget.AllViaServer)
                # 修改 Target 为 Broadcast
                broad_header = struct.pack(f'{ENDIAN}iii', msg_id, sender.player_id, ID_BROADCAST)
                broad_payload = broad_header + body
                broad_packet = struct.pack(f'{ENDIAN}i', len(broad_payload)) + broad_payload
                
                await self.broadcast(broad_packet, exclude_id=sender.player_id)

            except Exception as e:
                logger.error(f"RPC Decode Error: {e}")

        # 3. 其他消息
        else:
            # 默认逻辑：如果客户端发给 Server 但 Server 没处理，是否要广播？
            # 暂不处理
            pass

    async def broadcast(self, packet: bytes, exclude_id: int = None):
        """高效广播"""
        for pid, client in self.clients.items():
            if pid != exclude_id:
                await client.send_raw(packet)

    async def start(self, host, port):
        server = await asyncio.start_server(self.handle_connection, host, port)
        logger.info(f'Game Server Listening on {host}:{port}...')
        async with server:
            await server.serve_forever()

# ================= 入口 =================

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--host', default='0.0.0.0')
    parser.add_argument('--port', default=12345, type=int)
    args = parser.parse_args()
    
    game_server = GameServer()
    try:
        asyncio.run(game_server.start(args.host, args.port))
    except KeyboardInterrupt:
        logger.info("Server Stopped.")