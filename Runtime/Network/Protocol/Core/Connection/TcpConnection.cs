// Transport/TcpNetChannel.cs（修改版，不依赖 Pipelines）
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public class TcpNetChannel : INetChannel
    {
        public int ChannelId { get; }
        public bool IsActive => _socket?.Connected ?? false;
        
        public event Action<int, ushort, byte[]> OnFrameReceived;
        public event Action<int, string> OnError;

        private readonly Socket _socket;
        private readonly MemoryStream _recvStream; // 替代 Pipe
        private readonly byte[] _recvBuffer;
        private readonly object _lock = new();

        public TcpNetChannel(Socket socket, int channelId, int bufferSize = 8192)
        {
            _socket = socket;
            ChannelId = channelId;
            _socket.NoDelay = true;
            
            _recvStream = new MemoryStream();
            _recvBuffer = new byte[bufferSize];
            
            _ = ReceiveLoopAsync();
        }

        public void Send(ushort protocolId, byte[] payload)
        {
            if (!IsActive) return;
            
            byte[] frame = FrameCodec.Encode(protocolId, payload);
            try 
            { 
                _socket.Send(frame); 
            }
            catch (Exception ex) 
            { 
                OnError?.Invoke(ChannelId, $"Send failed: {ex.Message}");
                Dispose();
            }
        }

        private async UniTask ReceiveLoopAsync()
        {
            try
            {
                while (IsActive)
                {
                    int received = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(_recvBuffer), 
                        SocketFlags.None);
                    
                    if (received == 0) 
                    {
                        OnError?.Invoke(ChannelId, "Connection closed by remote");
                        break;
                    }

                    // 写入流并尝试解包
                    lock (_lock)
                    {
                        _recvStream.Write(_recvBuffer, 0, received);
                        ProcessStream();
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ChannelId, $"Receive error: {ex.Message}");
            }
            finally
            {
                Dispose();
            }
        }

        private void ProcessStream()
        {
            _recvStream.Position = 0;
            var buffer = _recvStream.GetBuffer();
            var readableBytes = (int)_recvStream.Length;
            int processed = 0;

            while (readableBytes - processed >= FrameCodec.HeaderSize)
            {
                var span = new ReadOnlySpan<byte>(buffer, processed, readableBytes - processed);
                int consumed = FrameCodec.TryDecode(span, out var protocolId, out var payload);
                
                if (consumed == 0) break; // 半包，等下次数据
                
                // 复制 payload（因为后面会修改流）
                var payloadCopy = new byte[payload.Length];
                payload.CopyTo(payloadCopy);
                
                OnFrameReceived?.Invoke(ChannelId, protocolId, payloadCopy);
                processed += consumed;
            }

            // 保留未处理的数据
            if (processed > 0)
            {
                int remaining = readableBytes - processed;
                if (remaining > 0)
                {
                    Buffer.BlockCopy(buffer, processed, buffer, 0, remaining);
                }
                _recvStream.SetLength(remaining);
                _recvStream.Position = remaining;
            }
        }

        public Task CloseAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try { _socket?.Close(); } catch { }
            _recvStream?.Dispose();
        }
    }
}