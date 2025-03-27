using UnityEngine;
using System;
using Unity.XR.CoreUtils.Bindings.Variables;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;

namespace Multiplayer
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        public Action<DiscoveryResponseData> OnServerFound;

        public Action<string> OnLobbyFailed;

        public IReadOnlyBindableVariable<string> Status
        {
            get => m_Status;
        }
        readonly BindableVariable<string> m_Status = new("");

        [SerializeField]
        ExampleNetworkDiscovery discovery;

        readonly Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();

        const string k_DebugPrepend = "<color=#EC0CFA>[Lobby Manager]</color> ";

        ushort defaultPort;
        string defaultIP;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            discovery.StartClient();
            discovery.ClientBroadcast(new DiscoveryBroadcastData());

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            defaultPort = transport.ConnectionData.Port;
            defaultIP = "0.0.0.0";
        }

        void SetDefaultConnectionData()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(defaultIP, defaultPort);
        }

        public void MyOnServerFound(IPEndPoint sender, DiscoveryResponseData response)
        {
            discoveredServers[sender.Address] = response;
            OnServerFound?.Invoke(response);
        }

        public void CreateLobby()
        {
            SetDefaultConnectionData();

            try
            {
                m_Status.Value = "Creating Lobby";

                string lobbyName = $"{NetworkGameManager.LocalPlayerName.Value}'s Table";
                discovery.ServerName = lobbyName;

                discovery.StopDiscovery();
                NetworkManager.Singleton.StartHost();
                discovery.StartServer();

                m_Status.Value = "Connected To Lobby";
            }
            catch (Exception e)
            {
                string failureMessage = "Failed to Create Lobby. Please try again.";
                Utils.Log($"{k_DebugPrepend}{failureMessage}\n\n{e}", 1);
                OnLobbyFailed?.Invoke(failureMessage);
            }
        }

        public void JoinLobby(DiscoveryResponseData lobby)
        {
            SetDefaultConnectionData();

            try
            {
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                IPAddress ip = IPAddress.Any;
                if (lobby.ServerName == "Default")
                {
                    ip = IPAddress.Parse("192.168.159.129");
                }
                else
                {
                    ip = discoveredServers.FirstOrDefault(
                        x => x.Value.Port == lobby.Port && x.Value.ServerName == lobby.ServerName
                    ).Key;
                }

                transport.SetConnectionData(ip.ToString(), lobby.Port);
                NetworkManager.Singleton.StartClient();

                m_Status.Value = "Connected To Lobby";
            }
            catch (Exception e)
            {
                string failureMessage = "Failed to Join Lobby.";
                Utils.Log($"{k_DebugPrepend}{e.Message}", 1);
                OnLobbyFailed?.Invoke($"{failureMessage}");
            }
        }

        public void RefreshLobbies()
        {
            discoveredServers.Clear();
            discovery.ClientBroadcast(new DiscoveryBroadcastData());
        }

        public void StartLobbyDiscovery()
        {
            discovery.StopDiscovery();
            discovery.StartClient();
        }

        public DiscoveryResponseData[] GetLobbies()
        {
            return discoveredServers.Values.ToArray();
        }

        public bool CanJoinLobby()
        {
            return !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost;
        }
    }
}
