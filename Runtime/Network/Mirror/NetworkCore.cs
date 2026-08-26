using Mirror;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// Mirror 接入层门面（v3.0.0 起取代自研 Protocol/ClientCore/ServerCore）。
    /// 统一暴露 Mirror 运行状态，业务代码不直接散落 Mirror API 调用。
    /// </summary>
    public static class NetworkCore
    {
        /// <summary>当前是否作为服务端运行（含 Host）。</summary>
        public static bool IsServer => NetworkServer.active;

        /// <summary>当前是否作为客户端运行（含 Host）。</summary>
        public static bool IsClient => NetworkClient.active;

        /// <summary>当前是否为 Host（服务端 + 客户端一体）。</summary>
        public static bool IsHost => NetworkServer.active && NetworkClient.active;
    }
}
