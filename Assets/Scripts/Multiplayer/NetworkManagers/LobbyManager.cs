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
        // Action that gets invoked when you fail to connect to a lobby. Primarily used for noting failure messages.
        public Action<string> OnLobbyFailed;

        /// <summary>
        /// Subscribe to this bindable string for status updates from this class
        /// </summary>
        public static IReadOnlyBindableVariable<string> status
        {
            get => m_Status;
        }
        readonly static BindableVariable<string> m_Status = new("");

        [SerializeField]
        static ExampleNetworkDiscovery discovery;
        static Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();

        const string k_DebugPrepend = "<color=#EC0CFA>[Lobby Manager]</color> ";


        public void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
        {
            discoveredServers[sender.Address] = response;
        }

        void Start()
        {
            discovery = FindFirstObjectByType<ExampleNetworkDiscovery>();
            discovery.StartClient();
            discovery.ClientBroadcast(new DiscoveryBroadcastData());
        }

        /// <summary>
        /// Quick Join Function will try and find any lobbies via QuickJoinLobbyAsync().
        /// If no lobbies are found then a new lobby is created.
        /// </summary>
        /// <returns></returns>
        public void CreateLobby()
        {
            try
            {
                m_Status.Value = "Creating Lobby";

                string lobbyName = $"{NetworkGameManager.LocalPlayerName.Value}'s Table";
                discovery.ServerName = lobbyName;
                NetworkManager.Singleton.StartHost();

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

        public void ReconnectToLobby()
        {
            if (Application.isPlaying)
            {
            }
        }

        public static void RefreshLobbies()
        {
            discoveredServers.Clear();
            discovery.ClientBroadcast(new DiscoveryBroadcastData());
        }

        public static DiscoveryResponseData[] GetLobbies()
        {
            return discoveredServers.Values.ToArray();
        }

        public static bool CanJoinLobby(DiscoveryResponseData lobby)
        {
            return !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost;
        }
    }
}
