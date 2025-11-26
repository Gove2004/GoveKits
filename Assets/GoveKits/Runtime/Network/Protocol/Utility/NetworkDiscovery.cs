using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Text;

namespace GoveKits.Network
{

    public class NetworkDiscovery : MonoBehaviour
    {
        [Header("Settings")]
        public int DiscoveryPort = 8899;
        public float BroadcastInterval = 1.0f;
        
        // 【新增】是否接收自己发出的广播（调试本机联机时勾选）
        public bool ReceiveSelfBroadcast = true; 

        public event Action<DiscoveryMessage, IPEndPoint> OnRoomFound;

        private UdpClient _udpClient;
        private bool _isRunning;
        private readonly byte[] _sendBuffer = new byte[1024];

        private void OnDisable() => StopDiscovery();

        public void StopDiscovery()
        {
            _isRunning = false;
            _udpClient?.Close();
            _udpClient = null;
        }

        #region Host: 发送广播

        public void StartBroadcasting(string roomInfo)
        {
            StopDiscovery();
            try
            {
                // 1. 尝试获取物理网卡，如果失败则回退到 Broadcast 地址
                var ipInfo = GetBestNetworkInterface();
                IPAddress targetIp = (ipInfo != null) ? CalculateBroadcastAddress(ipInfo) : IPAddress.Broadcast;

                // 2. 绑定 Socket
                _udpClient = new UdpClient();
                _udpClient.EnableBroadcast = true;
                
                // 【关键修改】不绑定特定 IP，直接绑定 Any，让系统决定路由
                // 这样本机 Loopback 也能收到
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

                Debug.Log($"[Discovery Host] Broadcasting to {targetIp}:{DiscoveryPort}...");

                // 3. 序列化
                var msg = new DiscoveryMessage(roomInfo);
                int length = 0;
                msg.Writing(_sendBuffer, ref length);
                
                byte[] data = new byte[length];
                Array.Copy(_sendBuffer, data, length);

                _isRunning = true;
                BroadcastLoop(data, new IPEndPoint(targetIp, DiscoveryPort)).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Discovery] Host Error: {e}");
                StopDiscovery();
            }
        }

        private async UniTaskVoid BroadcastLoop(byte[] data, IPEndPoint target)
        {
            while (_isRunning && _udpClient != null)
            {
                try
                {
                    // UDP 发送
                    await _udpClient.SendAsync(data, data.Length, target);
                }
                catch (Exception ex) { Debug.LogWarning($"[Discovery] Send warning: {ex.Message}"); }
                
                await UniTask.Delay(TimeSpan.FromSeconds(BroadcastInterval));
            }
        }

        #endregion

        #region Client: 接收广播

        public void StartListening()
        {
            StopDiscovery();
            try
            {
                // 1. 设置 UDP Client
                _udpClient = new UdpClient();
                
                // 【关键修改】允许端口复用，防止本机多开报错
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                // 绑定到任意地址 + 固定端口
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                
                _isRunning = true;
                Debug.Log($"[Discovery Client] Listening on port {DiscoveryPort}...");
                
                ListenLoop().Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Discovery] Client Error: {e}");
            }
        }

        private async UniTaskVoid ListenLoop()
        {
            while (_isRunning && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    
                    // 【关键修复】移除了 IP 过滤逻辑，或者根据 ReceiveSelfBroadcast 决定
                    // 如果你想在本机测试 Host 和 Client，必须允许接收自己
                    if (!ReceiveSelfBroadcast)
                    {
                         // 如果需要过滤自己，需要获取本机所有IP进行比对，比较繁琐且容易出错
                         // 建议在 DiscoveryMessage 内容里加个 Guid SessionID 来过滤，而不是靠 IP
                    }

                    // 2. 预检 ID
                    if (result.Buffer.Length < 4) continue;
                    int msgId = result.Buffer[0] | (result.Buffer[1] << 8) | (result.Buffer[2] << 16) | (result.Buffer[3] << 24);
                    
                    if (msgId == Protocol.DiscoveryID)
                    {
                        var msg = new DiscoveryMessage();
                        int index = 0;
                        msg.Reading(result.Buffer, ref index);
                        OnRoomFound?.Invoke(msg, result.RemoteEndPoint);
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { Debug.LogWarning($"[Discovery] Recv error: {ex.Message}"); }
            }
        }

        #endregion

        #region Helper (精简版)

        // 获取最佳网卡用于计算广播地址
        private UnicastIPAddressInformation GetBestNetworkInterface()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                if (networkInterface.OperationalStatus != OperationalStatus.Up) continue;

                var properties = networkInterface.GetIPProperties();
                foreach (var ip in properties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && 
                        !ip.Address.ToString().StartsWith("169.254") && 
                        !ip.Address.ToString().StartsWith("127."))
                    {
                        return ip;
                    }
                }
            }
            return null;
        }

        private IPAddress CalculateBroadcastAddress(UnicastIPAddressInformation unicastInfo)
        {
            if (unicastInfo == null || unicastInfo.IPv4Mask == null) return IPAddress.Broadcast;
            byte[] ipBytes = unicastInfo.Address.GetAddressBytes();
            byte[] maskBytes = unicastInfo.IPv4Mask.GetAddressBytes();
            byte[] broadcastBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            return new IPAddress(broadcastBytes);
        }

        #endregion
    }
}