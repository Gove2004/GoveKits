#!/usr/bin/env python3
"""
Simple Asyncio TCP Server for Unity NetSession
Protocol: Big-Endian (>), ID(int32) + Length(int32) + Body
"""
import asyncio
import struct
import argparse
import datetime

# ================= 配置区域 =================
# 对应 C# 里的 MsgID 定义
MSG_HEARTBEAT = 1
MSG_PLAYER_LOGIN = 1001

# 对应 C# BinaryData 的写入顺序 (msgId = packet[0]<<24...)
# '>' = Big-Endian (网络字节序)
# '<' = Little-Endian (如果你的 C# 用的是 BitConverter，请改成这个)
ENDIAN_FMT = '<' 
HEADER_FMT = f'{ENDIAN_FMT}ii' # int32 id, int32 len
HEADER_SIZE = 8
# ===========================================

def log(info):
    print(f"[{datetime.datetime.now().strftime('%H:%M:%S')}] {info}")

def build_packet(msg_id: int, body: bytes = b'') -> bytes:
    """打包数据包: Header + Body"""
    header = struct.pack(HEADER_FMT, msg_id, len(body))
    return header + body

async def handle_client(reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
    addr = writer.get_extra_info('peername')
    log(f'Client connected: {addr}')

    try:
        while True:
            # 1. 读取头部 (8字节)
            header = await reader.readexactly(HEADER_SIZE)
            msg_id, body_len = struct.unpack(HEADER_FMT, header)
            # print(f"Debug: MsgID={msg_id}, BodyLen={body_len}")

            # 2. 读取包体
            body = await reader.readexactly(body_len)

            # 3. 逻辑处理
            if msg_id == MSG_HEARTBEAT:
                # 收到心跳，通常服务器可以选择：
                # A. 什么都不做 (TCP本身可靠)
                # B. 回复一个心跳包 (Pong) -> 这里我们选择回复，证明链路通畅
                log(f"Recv Heartbeat from {addr}") # 频繁打印太吵，注释掉
                
                # 回复心跳
                writer.write(build_packet(MSG_HEARTBEAT))
                await writer.drain()

            elif msg_id == MSG_PLAYER_LOGIN:
                # 解析 PlayerData (假设结构: int ID + int NameLen + NameStr)
                # 注意：C# BinaryData.WriteInt 也是 Big-Endian
                if len(body) >= 8:
                    p_id = struct.unpack_from(f'{ENDIAN_FMT}i', body, 0)[0]
                    name_len = struct.unpack_from(f'{ENDIAN_FMT}i', body, 4)[0]
                    try:
                        p_name = body[8:8+name_len].decode('utf-8')
                        log(f"Player Login: ID={p_id}, Name={p_name}")
                        
                        # 这里可以模拟发送一个登录成功的包回去...
                    except Exception as e:
                        log(f"Decode name error: {e}")
                else:
                    log(f"Invalid Login Body Len: {len(body)}")

            else:
                log(f"Unknown MsgID={msg_id}, BodyLen={body_len}")

    except asyncio.IncompleteReadError:
        log(f'Client disconnected: {addr}')
    except ConnectionResetError:
        log(f'Connection reset: {addr}')
    except Exception as e:
        log(f'Error: {e}')
    finally:
        writer.close()
        await writer.wait_closed()

async def main(host, port):
    server = await asyncio.start_server(handle_client, host, port)
    log(f'Serving on {host}:{port} ...')
    async with server:
        await server.serve_forever()

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('host', nargs='?', default='127.0.0.1')
    parser.add_argument('port', nargs='?', default=12345, type=int)
    args = parser.parse_args()
    
    try:
        asyncio.run(main(args.host, args.port))
    except KeyboardInterrupt:
        print("\nServer stopped.")