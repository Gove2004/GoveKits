using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Text;

namespace GoveKits.Network
{
    // 请确保在 Protocol.cs 中定义了 public const int NetworkDiscoveryID = 2000;
    [Message(Protocol.NetworkDiscoveryID)]
    public class NetworkDiscoveryMessage : Message
    {
        // 建议格式: "房间名|地图|人数|最大人数|TCP端口"
        public string Info;

        public NetworkDiscoveryMessage() { }
        public NetworkDiscoveryMessage(string info)
        {
            Info = info;
        }

        protected override int BodyLength()
        {
            if (string.IsNullOrEmpty(Info)) return 4;
            return 4 + Encoding.UTF8.GetByteCount(Info);
        }

        protected override void BodyWriting(byte[] buffer, ref int index)
        {
            WriteString(buffer, Info ?? "", ref index);
        }

        protected override void BodyReading(byte[] buffer, ref int index)
        {
            Info = ReadString(buffer, ref index);
        }
    }





    
    public class NetworkDiscovery : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("UDP广播端口，必须与TCP游戏端口不同")]
        public int DiscoveryPort = 8899;  // 用于接受广播消息的UDP固定端口
        
        [Tooltip("广播间隔(秒)")]
        public float BroadcastInterval = 3.0f;

        // 事件：当客户端发现房间时触发 (消息内容, 房间IP)
        public event Action<NetworkDiscoveryMessage, IPEndPoint> OnRoomFound;

        private UdpClient _udpClient;
        private bool _isRunning;
        
        // 缓存：复用字节数组减少GC
        private readonly byte[] _sendBuffer = new byte[2048];
        
        // 缓存：本机IP，用于客户端过滤自己
        private string _localIpString;

        private void OnDisable()
        {
            StopDiscovery();
        }

        public void StopDiscovery()
        {
            _isRunning = false;
            if (_udpClient != null)
            {
                try { _udpClient.Close(); } catch { }
                _udpClient = null;
            }
        }

        #region Host: 发送广播

        public void StartBroadcasting(string roomInfo)
        {
            StopDiscovery();

            try
            {
                // 1. 获取最佳的物理网卡信息
                var ipInfo = GetBestNetworkInterface();
                if (ipInfo == null)
                {
                    Debug.LogError("[Discovery] 无法找到有效的局域网卡 (Check WiFi/Ethernet connection)");
                    return;
                }

                IPAddress localIp = ipInfo.Address;
                IPAddress broadcastIp = CalculateBroadcastAddress(ipInfo);

                Debug.Log($"[Discovery] Host IP: {localIp} | Broadcast Target: {broadcastIp}");

                // 2. 绑定到本机特定IP，强制从该网卡发送 (关键修改!)
                _udpClient = new UdpClient(new IPEndPoint(localIp, 0));
                _udpClient.EnableBroadcast = true;
                _udpClient.Ttl = 255; // 确保生存时间足够
                _isRunning = true;

                // 3. 准备消息数据
                var msg = new NetworkDiscoveryMessage(roomInfo);
                int length = 0;
                msg.Writing(_sendBuffer, ref length); // 序列化到 buffer
                
                byte[] finalBytes = new byte[length];
                Array.Copy(_sendBuffer, finalBytes, length);

                // 4. 启动循环
                BroadcastLoop(finalBytes, new IPEndPoint(broadcastIp, DiscoveryPort)).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Discovery] Start Host Failed: {e.Message}");
                StopDiscovery();
            }
        }

        private async UniTaskVoid BroadcastLoop(byte[] data, IPEndPoint target)
        {
            while (_isRunning && _udpClient != null)
            {
                try
                {
                    await _udpClient.SendAsync(data, data.Length, target);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Discovery] Send Warning: {ex.Message}");
                }

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
                // 获取本机IP用于后续过滤自己
                var localInfo = GetBestNetworkInterface();
                _localIpString = localInfo?.Address.ToString();

                // 客户端绑定到 Any (0.0.0.0)，这样能收到所有来源的广播
                _udpClient = new UdpClient(DiscoveryPort);
                _isRunning = true;

                Debug.Log($"[Discovery] Listening on port {DiscoveryPort}...");
                ListenLoop().Forget();
            }
            catch (SocketException e)
            {
                Debug.LogError($"[Discovery] Listen Failed (端口 {DiscoveryPort} 可能被占用): {e.Message}");
            }
        }

        private async UniTaskVoid ListenLoop()
        {
            while (_isRunning && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    
                    // 过滤自己发出的广播
                    if (result.RemoteEndPoint.Address.ToString() == _localIpString) 
                        continue;

                    ProcessPacket(result.Buffer, result.RemoteEndPoint);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Discovery] Receive Error: {ex.Message}");
                }
            }
        }

        private void ProcessPacket(byte[] data, IPEndPoint sender)
        {
            try
            {
                if (data.Length < 4) return;

                // 1. 预读 ID (假设是小端序，和 BinaryData 保持一致)
                int msgId = data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);

                if (msgId != Protocol.NetworkDiscoveryID) return;

                // 2. 反序列化
                var msg = new NetworkDiscoveryMessage();
                int index = 0;
                msg.Reading(data, ref index); // 调用基类完整读取逻辑

                // 3. 回调
                OnRoomFound?.Invoke(msg, sender);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Discovery] Parse Error from {sender}: {ex.Message}");
            }
        }

        #endregion

        #region 网络工具方法 (核心优化)

        /// <summary>
        /// 智能获取最佳网络接口 (过滤VMware/Docker/Loopback)
        /// </summary>
        private UnicastIPAddressInformation GetBestNetworkInterface()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 1. 类型过滤 (只选以太网和Wifi)
                if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                    continue;

                // 2. 状态过滤
                if (networkInterface.OperationalStatus != OperationalStatus.Up) 
                    continue;

                // 3. 名称关键词过滤 (虚拟网卡)
                string name = networkInterface.Description.ToLower();
                if (name.Contains("vmware") || name.Contains("virtual") || 
                    name.Contains("hyper-v") || name.Contains("docker") || 
                    name.Contains("vpn") || name.Contains("software loopback"))
                    continue;

                var properties = networkInterface.GetIPProperties();
                
                // 4. 网关过滤 (没有网关通常意味着没有连入局域网)
                if (properties.GatewayAddresses.Count == 0) continue;

                foreach (var addressInfo in properties.UnicastAddresses)
                {
                    // 5. 只取 IPv4
                    if (addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // 排除 169.254.x.x (Windows自动分配的无效IP)
                        if (addressInfo.Address.ToString().StartsWith("169.254")) continue;

                        return addressInfo; // 找到最佳匹配
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 根据IP和掩码，精确计算广播地址
        /// 公式: Broadcast = IP | (~SubnetMask)
        /// </summary>
        private IPAddress CalculateBroadcastAddress(UnicastIPAddressInformation unicastInfo)
        {
            if (unicastInfo?.IPv4Mask == null) return IPAddress.Broadcast; // 降级

            byte[] ipBytes = unicastInfo.Address.GetAddressBytes();
            byte[] maskBytes = unicastInfo.IPv4Mask.GetAddressBytes();
            byte[] broadcastBytes = new byte[ipBytes.Length];

            if (ipBytes.Length != maskBytes.Length) return IPAddress.Broadcast;

            for (int i = 0; i < ipBytes.Length; i++)
            {
                // 核心位运算
                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            }

            return new IPAddress(broadcastBytes);
        }

        #endregion
    }
}