using System;
using System.Net;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 网络通道抽象接口。
    /// 上层无需关心底层传输细节，只需处理吐出的完整帧数据。
    /// </summary>
    public interface IChannel : IDisposable
    {
        /// <summary>
        /// 通道是否处于已连接状态。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 远程端点的网络地址信息。
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 通道成功建立连接时触发。
        /// </summary>
        event Action<IChannel> OnConnected;

        /// <summary>
        /// 通道断开连接时触发，参数为断开原因。
        /// </summary>
        event Action<IChannel, string> OnDisconnected;

        /// <summary>
        /// 收到原始字节数据时触发，参数为帧数据。
        /// </summary>
        event Action<IChannel, byte[]> OnDataReceived;

        /// <summary>
        /// 异步连接到目标端点。
        /// </summary>
        /// <param name="target">目标网络端点。</param>
        /// <returns>连接成功返回 true，失败返回 false。</returns>
        System.Threading.Tasks.Task<bool> ConnectAsync(EndPoint target);

        /// <summary>
        /// 发送字节数据到远端。
        /// </summary>
        /// <param name="data">要发送的字节数组。</param>
        void Send(byte[] data);

        /// <summary>
        /// 关闭通道并释放资源。
        /// </summary>
        /// <param name="reason">断开原因说明。</param>
        void Close(string reason = "");
    }
}
