using Mirror;

namespace GoveKits.Runtime.Network
{
    /// <summary>网络运行状态。</summary>
    public enum NetworkState
    {
        /// <summary>未运行（服务端与客户端均未激活）。</summary>
        Offline,
        /// <summary>仅服务端（纯服务端模式）。</summary>
        ServerOnly,
        /// <summary>仅客户端（纯客户端模式）。</summary>
        ClientOnly,
        /// <summary>服务端 + 客户端一体（Host 模式）。</summary>
        Host
    }

    /// <summary>
    /// Mirror 接入层门面（v3.0.0 起取代自研 Protocol/ClientCore/ServerCore）。
    /// 统一暴露 Mirror 运行状态与常用查询，业务代码不直接散落 Mirror API 调用。
    /// </summary>
    public static class NetworkCore
    {
        /// <summary>当前网络运行状态（离线/纯服务端/纯客户端/Host）。</summary>
        public static NetworkState State
        {
            get
            {
                bool server = NetworkServer.active;
                bool client = NetworkClient.active;
                if (server && client) return NetworkState.Host;
                if (server) return NetworkState.ServerOnly;
                if (client) return NetworkState.ClientOnly;
                return NetworkState.Offline;
            }
        }

        /// <summary>当前是否作为服务端运行（含 Host）。</summary>
        public static bool IsServer => NetworkServer.active;

        /// <summary>当前是否作为客户端运行（含 Host）。</summary>
        public static bool IsClient => NetworkClient.active;

        /// <summary>当前是否为 Host（服务端 + 客户端一体）。</summary>
        public static bool IsHost => NetworkServer.active && NetworkClient.active;

        /// <summary>
        /// 客户端往返延迟（秒）。仅在客户端连接就绪时有意义，
        /// 未运行客户端或未连接时返回 0（调用方应结合 <see cref="IsClientConnected"/> 判断有效性）。
        /// </summary>
        public static double Rtt => IsClientConnected ? NetworkTime.rtt : 0d;

        /// <summary>客户端是否已建立连接（含 Host 的本地客户端）。</summary>
        public static bool IsClientConnected => NetworkClient.active && NetworkClient.connection != null;

        /// <summary>服务端在线连接数（非服务端模式返回 0）。</summary>
        public static int ConnectionCount => NetworkServer.active ? NetworkServer.connections?.Count ?? 0 : 0;
    }
}
