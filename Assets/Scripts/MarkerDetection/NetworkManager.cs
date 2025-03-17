using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using System.Net;
using System.Linq;
using ExitGames.Client.Photon;
using MyTools;

public partial class GameManager
{
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        EnablePoseTrackingComponents(false);
        print("ARCamera local pos: " + ARCamera.transform.localPosition);
        print("ARCamera local: " + ARCamera.transform.localRotation.eulerAngles);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.UseRpcMonoBehaviourCache = true;
        PhotonNetwork.EnableCloseConnection = true;
    }

    private void OnApplicationQuit()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
            PhotonNetwork.CurrentRoom.PlayerTtl = 0;

            //foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            //{
            //    if (!player.IsMasterClient)
            //    {
            //        PhotonNetwork.CloseConnection(player);
            //        PhotonNetwork.SendAllOutgoingCommands();
            //    }
            //}
        }
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        if (!PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinOrCreateRoom("selabi87", new RoomOptions { IsOpen = true, IsVisible = true, MaxPlayers = 0 }, null, null);
        }
    }

    public override void OnDisconnected(DisconnectCause disconnectCause)
    {
        base.OnDisconnected(disconnectCause);
        Application.Quit();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        Application.Quit();
    }

    public override void OnJoinedRoom()
    {


        InitHololens();

        var key = "LocalIP";
        var dssSignalingServer = "";
        if (PhotonNetwork.IsMasterClient)
        {
            dssSignalingServer = GetLocalIPv4();
            Hashtable roomProperties = new Hashtable();
            roomProperties[key] = dssSignalingServer;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }
        else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
        {
            dssSignalingServer = PhotonNetwork.CurrentRoom.CustomProperties[key].ToString();
        }
    }


    public string GetLocalIPv4()
    {
        var ip = Dns.GetHostEntry(Dns.GetHostName())
        .AddressList.First(
            f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .ToString();
        return ip;
    }


}
