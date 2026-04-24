using System;
using System.Net;
using System.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public interface IConnection : IDisposable
    {
        bool IsConnected { get; }
        EndPoint RemoteEndPoint { get; }

        event Action<IConnection> OnConnected;
        event Action<IConnection, string> OnDisconnected;
        
        // 【核心】：吐出的直接是完整的帧数据，上层不再需要关心粘包和半包
        event Action<IConnection, byte[]> OnFrameReceived;

        Task<bool> ConnectAsync(EndPoint target);
        
        // 【核心】：传入的数据会被底层自动盖上长度头并发送
        void SendFrame(byte[] frameData);
        
        void Close(string reason = "");
    }
}