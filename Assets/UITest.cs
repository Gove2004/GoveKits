using System.Collections.Generic;
using System.Net;
using GoveKits.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // 用于 First() 方法

public class UITest : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button joinButton;     // 搜索按钮
    public Button stopButton;     // 停止搜索
    public Button connectButton;  // 连接按钮

    public TextMeshProUGUI logText;
    public TextMeshProUGUI roomsText;
    public TextMeshProUGUI myIDText;

    [Header("Components")]
    public NetworkDiscovery discovery;

    // key: IP:Port 字符串 (保证唯一性) -> value: (Info, EndPoint)
    private Dictionary<string, RoomInfo> _discoveredRooms = new Dictionary<string, RoomInfo>();

    private struct RoomInfo
    {
        public string RawInfo;
        public IPEndPoint EndPoint;
        public int TcpPort; // 解析出的游戏端口
    }

    private void Start()
    {
        // UI 绑定
        hostButton.onClick.AddListener(OnHostButton);
        joinButton.onClick.AddListener(OnJoinButton);
        stopButton.onClick.AddListener(OnStopButton);
        connectButton.onClick.AddListener(OnConnectButton);

        // 发现事件
        discovery.OnRoomFound += OnHostFound;

        // 网络状态事件 (让 Log 也能显示连接结果)
        if (NetworkManager.Instance)
        {
            NetworkManager.Instance.OnClientConnected += (id) => Log($"<color=green>Connected! My ID: {id}</color>");
            NetworkManager.Instance.OnClientDisconnected += (id) => Log($"<color=red>Client {id} Disconnected</color>");
            NetworkManager.Instance.OnServerDisconnected += () => Log($"<color=red>Disconnected from Server</color>");
        }
    }


    public void Update()
    {
        // 实时显示我的玩家 ID
        if (NetworkManager.Instance)
        {
            myIDText.text = $"My Player ID: {NetworkManager.Instance.MyPlayerID}";
        }
    }

    private void OnDestroy()
    {
        if(discovery) discovery.OnRoomFound -= OnHostFound;
    }

    #region Button Events

    public void OnHostButton()
    {
        Log("Starting as Host...");
        
        // 1. 先启动 Host，确保 TCP 端口监听成功
        // 注意：如果你之前配置 NetworkManager.Instance.Port = 0 (随机端口)，
        // 这里需要获取实际绑定的端口。目前假设是固定配置。
        NetworkManager.Instance.StartHost();
        int gamePort = NetworkManager.Instance.Port;

        // 2. 广播房间信息: "房间名|地图|游戏TCP端口"
        // 建议加上随机数防止名字重复，方便测试
        string roomName = $"Room_{Random.Range(10,99)}";
        string info = $"{roomName}|Map1|{gamePort}"; 
        
        discovery.StartBroadcasting(info);
        Log($"Broadcasting: {info}");
    }

    public void OnJoinButton()
    {
        Log("Searching for Rooms...");
        _discoveredRooms.Clear();
        UpdateRoomsText();
        discovery.StartListening();
        
        // 禁用 Host 按钮防止误触
        hostButton.interactable = false;
    }

    public void OnStopButton()
    {
        Log("Stopping Discovery...");
        NetworkManager.Instance.Close();
        discovery.StopDiscovery();
        hostButton.interactable = true;
    }

    public void OnConnectButton()
    {
        if (_discoveredRooms.Count == 0)
        {
            Log("No rooms found yet.");
            return;
        }

        // 简单策略：连接列表里的第一个房间
        var targetRoom = _discoveredRooms.Values.First();
        
        string targetIp = targetRoom.EndPoint.Address.ToString();
        int targetTcpPort = targetRoom.TcpPort;

        Log($"Connecting to {targetIp}:{targetTcpPort} ...");

        // stop discovery before connecting
        discovery.StopDiscovery();

        // 【关键修复】设置 NetworkManager 的目标 IP 和 Port
        NetworkManager.Instance.IP = targetIp;
        NetworkManager.Instance.Port = targetTcpPort;

        // 启动客户端
        NetworkManager.Instance.StartClient();
    }

    #endregion

    #region Callbacks

    // 注意：OnRoomFound 可能在 UniTask 线程触发，建议回到主线程处理
    public void OnHostFound(DiscoveryMessage msg, IPEndPoint senderEndpoint)
    {
        // 解析信息字符串 "Name|Map|Port"
        string[] parts = msg.Info.Split('|');
        if (parts.Length < 3) return;

        int tcpPort;
        if (!int.TryParse(parts[2], out tcpPort)) return;

        // 使用 sender 的 IP 加上字符串里的端口作为唯一 Key
        // 因为 UDP 广播端口(8899) 和 游戏 TCP 端口(12345) 不一样
        string key = $"{senderEndpoint.Address}:{tcpPort}";

        if (!_discoveredRooms.ContainsKey(key))
        {
            Log($"[Discovery] Found: {parts[0]} ({senderEndpoint.Address})");

            _discoveredRooms.Add(key, new RoomInfo
            {
                RawInfo = msg.Info,
                EndPoint = senderEndpoint,
                TcpPort = tcpPort
            });

            UpdateRoomsText();
        }
    }

    #endregion

    #region UI Helper

    public void UpdateRoomsText()
    {
        roomsText.text = "<b>Discovered Rooms:</b>\n";
        foreach (var room in _discoveredRooms.Values)
        {
            // 显示：RoomName - IP:Port
            string[] parts = room.RawInfo.Split('|');
            string name = parts[0];
            string map = parts[1];
            
            roomsText.text += $"> {name} [{map}] - {room.EndPoint.Address}:{room.TcpPort}\n";
        }
    }

    public void Log(string message)
    {
        // 简单的时间戳
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        logText.text += $"[{time}] {message}\n";
        
        // 自动滚动到底部 (如果 TextMeshPro 放在 ScrollView 里，这里需要操作 ScrollRect)
        // 这里简单做截断防止文本过长
        if (logText.text.Length > 2000)
        {
            logText.text = logText.text.Substring(logText.text.Length - 2000);
        }
    }

    #endregion
}