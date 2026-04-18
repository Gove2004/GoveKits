// using System;
// using System.Net;
// using System.Net.Sockets;
// using UnityEngine;
// using Cysharp.Threading.Tasks;
// using GoveKits.Runtime.Core;
// using MessagePack;

// namespace GoveKits.Runtime.Network
// {
//     [MessagePackObject]
//     public class DiscoveryMsg : IMessage
//     {
        
//     }



//     public class NetworkDiscovery : MonoBehaviour
//     {
//         [Header("Settings")]
//         public int DiscoveryPort = 8899; // 专门用于 UDP 广播的端口
//         public float BroadcastInterval = 1.0f;

//         // 接收到房间时的回调
//         public event Action<DiscoveryMsg, IPEndPoint> OnRoomFound;

//         private UdpClient _udpClient;
//         private bool _isRunning;
        
//         // 本次会话的唯一标识，用于区分是不是自己发的包
//         private string _sessionGuid; 

//         private void Awake()
//         {
//             _sessionGuid = System.Guid.NewGuid().ToString();
//         }

//         private void OnDisable() => StopDiscovery();

//         public void StopDiscovery()
//         {
//             _isRunning = false;
//             _udpClient?.Close();
//             _udpClient = null;
//         }

//         #region Host: 发送广播

//         public void StartBroadcasting(string roomName, int gamePort, int curPlayers, int maxPlayers)
//         {
//             StopDiscovery();
            
//             try
//             {
//                 _udpClient = new UdpClient();
//                 _udpClient.EnableBroadcast = true;
                
//                 // 绑定到任意端口发送
//                 _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

//                 LogCore.Debug("Discovery", $"Start broadcasting on port {DiscoveryPort}...");

//                 // 1. 构造 Protobuf 消息
//                 var msg = new DiscoveryMsg
//                 {
//                     // Port = gamePort,
//                     // RoomName = roomName,
//                     // HostGuid = _sessionGuid, // 关键：放入自己的 ID
//                     // CurrentPlayers = curPlayers,
//                     // MaxPlayers = maxPlayers
//                 };

//                 // 2. 序列化 (手动拼装 UDP 包: MsgID + Body)
//                 byte[] packet = PackUdpMessage(msg);

//                 _isRunning = true;
//                 // 广播目标地址: 255.255.255.255
//                 BroadcastLoop(packet, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort)).Forget();
//             }
//             catch (Exception e)
//             {
//                 LogCore.Error("Discovery", $"Host Error: {e.Message}");
//                 StopDiscovery();
//             }
//         }

//         private async UniTaskVoid BroadcastLoop(byte[] data, IPEndPoint target)
//         {
//             while (_isRunning && _udpClient != null)
//             {
//                 try
//                 {
//                     await _udpClient.SendAsync(data, data.Length, target);
//                 }
//                 catch (Exception ex) 
//                 { 
//                     // 忽略一些网络不可达的临时错误
//                     LogCore.Warn("Discovery", $"Send warning: {ex.Message}"); 
//                 }
                
//                 await UniTask.Delay(TimeSpan.FromSeconds(BroadcastInterval));
//             }
//         }

//         #endregion

//         #region Client: 接收广播

//         public void StartListening()
//         {
//             StopDiscovery();
//             try
//             {
//                 _udpClient = new UdpClient();
                
//                 // 允许端口复用 (关键：防止同一台机器开多个客户端时报错)
//                 _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
//                 // 监听所有网卡的 DiscoveryPort
//                 _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                
//                 _isRunning = true;
//                 LogCore.Debug("Discovery", "Listening for rooms...");
                
//                 ListenLoop().Forget();
//             }
//             catch (Exception e)
//             {
//                 LogCore.Warn("Discovery", $"Client Error: {e.Message}");
//             }
//         }

//         private async UniTaskVoid ListenLoop()
//         {
//             while (_isRunning && _udpClient != null)
//             {
//                 try
//                 {
//                     // var result = await _udpClient.ReceiveAsync();
                    
//                     // // 1. 解析包
//                     // // 格式: [MsgID(4)] + [Body(N)]
//                     // if (result.Buffer.Length < 4) continue;

//                     // int msgId = BitConverter.ToInt32(result.Buffer, 0); // 假设是 Little Endian
                    
//                     // // 2. 校验 ID 是否为 DiscoveryMsg
//                     // // 这里可以直接硬编码 ID，或者从 Registry 获取
//                     // if (msgId != 999) continue;

//                     // // 3. 解析 Body
//                     // // 直接传 数组, 偏移量, 长度
//                     // int offset = 4;
//                     // int length = result.Buffer.Length - 4;
//                     // var msg = DiscoveryMsg.Parser.ParseFrom(result.Buffer, offset, length);

//                     // // 4. 【关键】过滤自己
//                     // // 如果消息里的 GUID 和我的一样，说明是我自己发的广播，忽略
//                     // if (msg.HostGuid == _sessionGuid) continue;

//                     // // 5. 触发事件
//                     // OnRoomFound?.Invoke(msg, result.RemoteEndPoint);
//                 }
//                 catch (ObjectDisposedException) { break; }
//                 catch (Exception ex) 
//                 { 
//                     LogCore.Warn("Discovery", $"Recv error: {ex.Message}"); 
//                 }
//             }
//         }

//         #endregion

//         #region Helper

//         // 简单的 UDP 打包工具：[MsgID 4字节] + [Protobuf 数据]
//         private byte[] PackUdpMessage(IMessage msg)
//         {
//             int msgId = 999;
//             int bodySize = msg.CalculateSize();
//             byte[] packet = new byte[4 + bodySize];

//             // 写入 MsgID (Little Endian)
//             packet[0] = (byte)(msgId & 0xFF);
//             packet[1] = (byte)((msgId >> 8) & 0xFF);
//             packet[2] = (byte)((msgId >> 16) & 0xFF);
//             packet[3] = (byte)((msgId >> 24) & 0xFF);

//             // 写入 Body
//             var span = new Span<byte>(packet, 4, bodySize);
//             msg.WriteTo(span);

//             return packet;
//         }

//         #endregion
//     }
// }