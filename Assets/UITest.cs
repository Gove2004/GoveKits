using System.Collections.Generic;
using System.Net;
using GoveKits.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITest : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public Button stopButton;
    public Button connectButton;

    public NetworkDiscovery discovery;


    public TextMeshProUGUI logText;
    public TextMeshProUGUI roomsText;
    
    public Dictionary<string, IPEndPoint> discoveredRooms = new();

    public void Start()
    {
        hostButton.onClick.AddListener(OnHostButton);
        joinButton.onClick.AddListener(OnJoinButton);
        stopButton.onClick.AddListener(OnStopButton);
        connectButton.onClick.AddListener(OnConnectButton);

        discovery.OnRoomFound += OnHostFound;
    }


    public void OnHostButton()
    {
        Log("Starting as Host...");
        string info = $"MyRoom|Map1|{NetworkManager.Instance.Port}"; 
        discovery.StartBroadcasting(info);

        NetworkManager.Instance.StartHost();
    }

    public void OnJoinButton()
    {
        Log("Searching for Rooms...");
        discovery.StartListening();
    }

    public void OnStopButton()
    {
        Log("Stopping Discovery...");
        discovery.StopDiscovery();
        
        roomsText.text = "Discovered Rooms:\n";
        discoveredRooms.Clear();
    }


    public void OnConnectButton()
    {
        if (discoveredRooms.Count == 0)
        {
            Log("No rooms to connect to.");
            return;
        }

        // 连接到第一个发现的房间
        var firstRoomKey = new List<string>(discoveredRooms.Keys)[0];
        var firstRoom = discoveredRooms[firstRoomKey];
        string ip = firstRoom.Address.ToString();
        int port = int.Parse(firstRoomKey.Split('|')[2]);

        Log($"Connecting to {ip}:{port} ...");
        NetworkManager.Instance.StartClient();
    }


    public void OnHostFound(NetworkDiscoveryMessage msg, IPEndPoint endpoint)
    {
        Log($"[Discovery] Found Room: {msg.Info} at {endpoint.Address}:{endpoint.Port}");
        
        if (!discoveredRooms.ContainsKey(msg.Info))
        {
            discoveredRooms.Add(msg.Info, endpoint);
            UpdateRoomsText();
        }
    }




    public void UpdateRoomsText()
    {
        roomsText.text = "Discovered Rooms:\n";
        foreach (var room in discoveredRooms)
        {
            roomsText.text += $"{room.Key} - {room.Value.Address}:{room.Value.Port}\n";
        }
    }





    public void Log(string message)
    {
        logText.text += message + "\n";
    }

}