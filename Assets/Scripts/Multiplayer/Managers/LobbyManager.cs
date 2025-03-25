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

        public Action<string> OnLobbyFailed;

        public IReadOnlyBindableVariable<string> Status
        {
            get => m_Status;
        }
        readonly BindableVariable<string> m_Status = new("");

        [field: SerializeField]
        public ExampleNetworkDiscovery Discovery { get; private set; }

        readonly Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();

        const string k_DebugPrepend = "<color=#EC0CFA>[Lobby Manager]</color> ";

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
            Discovery.StartClient();
            Discovery.ClientBroadcast(new DiscoveryBroadcastData());
        }

        public void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
        {
            discoveredServers[sender.Address] = response;
        }

        public void CreateLobby()
        {
            try
            {
                m_Status.Value = "Creating Lobby";

                string lobbyName = $"{NetworkGameManager.LocalPlayerName.Value}'s Table";
                Discovery.ServerName = lobbyName;

                Discovery.StopDiscovery();
                NetworkManager.Singleton.StartHost();
                Discovery.StartServer();

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
            try
            {
                UnityTransport transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

                var ip = discoveredServers.FirstOrDefault(
                    x => x.Value.Port == lobby.Port && x.Value.ServerName == lobby.ServerName
                ).Key;

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
            Discovery.ClientBroadcast(new DiscoveryBroadcastData());
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
