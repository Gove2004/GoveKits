#!/usr/bin/env python3
"""
Simple asyncio TCP server for testing the Unity NetSocket protocol:
Message format: [msgID:int32 little][bodyLen:int32 little][body bytes]
This server will:
 - accept multiple clients
 - print received messages (msgID and raw body)
 - periodically (every 5s) send a sample PlayerMessage (msgID=1001)

Run: python python_tcp_server.py 127.0.0.1 12345
"""
import asyncio
import sys
import struct
import argparse


def build_player_message(player_id: int, name: str) -> bytes:
    # PlayerData: int PlayerId + string (int length + utf8 bytes)
    name_bytes = name.encode('utf-8')
    # body: PlayerId (4 bytes) + NameLength (4 bytes) + name bytes
    body = struct.pack('<i', player_id) + struct.pack('<i', len(name_bytes)) + name_bytes
    msg_id = 1001
    header = struct.pack('<ii', msg_id, len(body))
    return header + body


async def handle_client(reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
    addr = writer.get_extra_info('peername')
    print(f'Client connected: {addr}')

    # Start a background task to periodically send a sample PlayerMessage
    async def periodic_send():
        try:
            while not writer.is_closing():
                data = build_player_message(42, 'ServerAlice')
                writer.write(data)
                await writer.drain()
                await asyncio.sleep(5)
        except Exception as e:
            print('Periodic send error:', e)

    send_task = asyncio.create_task(periodic_send())

    try:
        while True:
            # Read header (8 bytes)
            header = await reader.readexactly(8)
            msg_id, body_len = struct.unpack('<ii', header)
            # Read body
            body = await reader.readexactly(body_len)
            print(f'Recv from {addr}: msg_id={msg_id}, body_len={body_len}, body={body}')

            # For demonstration, echo back the same message id with player id incremented
            if msg_id == 1001:
                # parse player id and name
                if body_len >= 8:
                    player_id = struct.unpack_from('<i', body, 0)[0]
                    name_len = struct.unpack_from('<i', body, 4)[0]
                    try:
                        name = body[8:8+name_len].decode('utf-8')
                    except Exception:
                        name = '<decode error>'
                    print(f'Parsed PlayerMessage: id={player_id}, name={name}')
                    # send back acknowledgement with incremented id
                    resp = build_player_message(player_id + 1, name + '_srv')
                    writer.write(resp)
                    await writer.drain()
            else:
                # echo generic
                writer.write(header + body)
                await writer.drain()

    except asyncio.IncompleteReadError:
        print(f'Client disconnected: {addr}')
    except Exception as e:
        print(f'Connection error ({addr}):', e)
    finally:
        send_task.cancel()
        try:
            await send_task
        except Exception:
            pass
        writer.close()
        await writer.wait_closed()


async def main(host: str, port: int):
    server = await asyncio.start_server(handle_client, host, port)
    addr = server.sockets[0].getsockname()
    print(f'Serving on {addr}')
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
        print('Server stopped')
