// Transport/INetChannel.cs
using System;
using System.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 网络通道抽象（替代 IConnection，语义更准确）
    /// </summary>
    public interface INetChannel : IDisposable
    {
        int ChannelId { get; }
        bool IsActive { get; }
        
        event Action<int, ushort, byte[]> OnDataReceived; // channelId, protocolId, payload
        event Action<int, string> OnError; // channelId, reason
        
        void SetID(int id);
        void Send(ushort protocolId, byte[] payload);
        Task CloseAsync();
    }
}