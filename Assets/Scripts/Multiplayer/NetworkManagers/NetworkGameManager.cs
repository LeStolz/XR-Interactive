using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using Unity.Services.Lobbies;
using UnityEditor;
using System.Collections;
using UnityEngine.XR.Management;

namespace Multiplayer
{
#if USE_FORCED_BYTE_SERIALIZATION
    class ForceByteSerialization : NetworkBehaviour
    {
        NetworkVariable<byte> m_ForceByteSerialization;
    }
#endif

    [RequireComponent(typeof(LobbyManager)), RequireComponent(typeof(AuthenticationManager))]
    public class NetworkGameManager : NetworkBehaviour
    {
        public enum ConnectionState
        {
            None,
            Authenticating,
            Authenticated,
            Connecting,
            Connected
        }

        public const int maxPlayers = 6;

        public static NetworkGameManager Instance { get; private set; }
        public static ulong LocalId;
        public static string AuthenicationId;

        public static BindableVariable<string> LocalPlayerName = new("");
        public static IReadOnlyBindableVariable<bool> Connected
        {
            get => connected;
        }
        static BindableVariable<bool> connected = new BindableVariable<bool>(false);

        public static IReadOnlyBindableVariable<ConnectionState> CurrentConnectionState
        {
            get => connectionState;
        }
        static BindableEnum<ConnectionState> connectionState = new BindableEnum<ConnectionState>(ConnectionState.None);

        public Action<ulong, bool> playerStateChanged;
        public Action<string> connectionUpdated;
        public Action<string> connectionFailedAction;

        public LobbyManager LobbyManager { get; private set; }

        public AuthenticationManager AuthenticationManager { get; private set; }



        readonly List<ulong> currentPlayerIDs = new();
        NetworkList<NetworkedRole> networkedRoles;
        [field: SerializeField]
        public GameObject MRInteractionSetup { get; private set; }
        [SerializeField]
        Canvas WorldSpaceCanvas;
        [SerializeField]
        Canvas ScreenSpaceCanvas;
        [field: SerializeField]
        public GameObject TableUI { get; private set; }
        [SerializeField]
        int tableScale;



        [SerializeField]
        RoleButton[] roleButtons;
        public Role localRole;

        bool isShuttingDown = false;
        const string debugPrepend = "<color=#FAC00C>[Network Game Manager]</color> ";

        protected virtual async void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            networkedRoles = new NetworkList<NetworkedRole>();

            LocalPlayerName.Value = "";

            if (TryGetComponent(out LobbyManager lobbyManager) && TryGetComponent(out AuthenticationManager authenticationManager))
            {
                LobbyManager = lobbyManager;
                AuthenticationManager = authenticationManager;
                LobbyManager.OnLobbyFailed += ConnectionFailed;
            }
            else
            {
                enabled = false;
                return;
            }

#if UNITY_EDITOR
            if (!CloudProjectSettings.projectBound)
            {
                Utils.Log($"{debugPrepend}Project has not been linked to Unity Cloud.", 2);
                return;
            }
#endif

            connected.Value = false;
            connectionState.Value = ConnectionState.Authenticating;

            bool signedIn = await Authenticate();
            if (!signedIn)
            {
                Utils.Log($"{debugPrepend}Failed to Authenticate.", 1);
                ConnectionFailed("Failed to Authenticate.");
                PlayerHudNotification.Instance.ShowText($"Failed to Authenticate.");
            }
            else
            {
                connectionState.Value = ConnectionState.Authenticated;
            }
        }

        protected virtual void Start()
        {
            NetworkManager.Singleton.OnClientStopped += OnLocalClientStopped;

            if (localRole != Role.HMD)
            {
                TableUI.transform.SetParent(ScreenSpaceCanvas.transform, false);
                TableUI.transform.localScale = new Vector3(tableScale, tableScale, tableScale);
                TableUI.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            if (localRole == Role.HMD || localRole == Role.ServerTracker)
            {
                StartCoroutine(StartXR());
            }
        }

        IEnumerator StartXR()
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }

            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
            }
            else
            {
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ShutDown();
        }

        private void OnApplicationQuit()
        {
            ShutDown();
        }

        async void ShutDown()
        {
            if (isShuttingDown) return;
            isShuttingDown = true;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientStopped -= OnLocalClientStopped;
            }

            await LobbyManager.RemovePlayerFromLobby(AuthenicationId);
        }

        public async Task<bool> Authenticate()
        {
            return await AuthenticationManager.Authenticate();
        }
        public bool IsAuthenticated()
        {
            return AuthenticationManager.IsAuthenticated();
        }

        /// <summary>
        /// Called from XRINetworkPlayer once they have spawned.
        /// </summary>
        public virtual void LocalPlayerConnected(ulong localPlayerId)
        {
            LocalId = localPlayerId;
            connected.Value = true;
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Connected");
        }

        protected virtual void OnLocalClientStopped(bool id)
        {
            connected.Value = false;
            currentPlayerIDs.Clear();
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Disconnected");

            if (IsAuthenticated())
            {
                connectionState.Value = ConnectionState.Authenticated;
            }
            else
            {
                connectionState.Value = ConnectionState.None;
            }
        }

        public virtual bool TryGetPlayerByID(ulong id, out NetworkPlayer player)
        {
            player = null;

            if (NetworkManager.ConnectedClients.TryGetValue(id, out var client))
            {
                if (client.PlayerObject == null || !client.PlayerObject.TryGetComponent(out NetworkPlayer p))
                {
                    player = FindPlayerByReference(id);
                    if (player != null)
                    {
                        client.PlayerObject = player.NetworkObject;
                        return true;
                    }
                }
                else
                {
                    player = p;
                    return true;
                }
            }

            Utils.LogWarning($"Player with id {id} is not an active client");
            return false;
        }

        NetworkPlayer FindPlayerByReference(ulong id)
        {
            NetworkPlayer[] allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);

            foreach (NetworkPlayer p in allPlayers)
            {
                if (p.NetworkObject.OwnerClientId == id)
                {
                    return p;
                }
            }
            Debug.LogError($"Player with id {id} not found");
            return null;
        }

        /// <summary>
        /// This function will set the player ID in the list <see cref="currentPlayerIDs"/> and
        /// invokes the callback <see cref="playerStateChanged"/>.
        /// </summary>
        /// <param name="playerID"><see cref="NetworkObject.OwnerClientId"/> of the joined player.</param>
        /// <remarks>Called from <see cref="NetworkPlayer.CompleteSetup"/>.</remarks>
        public virtual void PlayerJoined(ulong playerID)
        {
            // If playerID is not already registered, then add.
            if (!currentPlayerIDs.Contains(playerID))
            {
                currentPlayerIDs.Add(playerID);
                playerStateChanged?.Invoke(playerID, true);
            }
            else
            {
                Utils.Log($"{debugPrepend}Trying to Add a player ID [{playerID}] that already exists", 1);
            }
        }

        /// <summary>
        /// Called from <see cref="NetworkPlayer.OnDestroy"/>.
        /// </summary>
        /// <param name="playerID"><see cref="NetworkObject.OwnerClientId"/> of the player who left.</param>
        public virtual void PlayerLeft(ulong playerID)
        {
            // Check to make sure player has been registerd.
            if (currentPlayerIDs.Contains(playerID))
            {
                currentPlayerIDs.Remove(playerID);
                playerStateChanged?.Invoke(playerID, false);
            }
            else
            {
                Utils.Log($"{debugPrepend}Trying to remove a player ID [{playerID}] that doesn't exist", 1);
            }
        }

        public virtual void ConnectionFailed(string reason)
        {
            connectionFailedAction?.Invoke(reason);
            connectionState.Value = Multiplayer.AuthenticationManager.IsAuthenticated() ? ConnectionState.Authenticated : ConnectionState.None;
        }

        public virtual void ConnectionUpdated(string update)
        {
            connectionUpdated?.Invoke(update);
        }

        public virtual async void QuickJoinLobby()
        {
            if (await AbleToConnect())
            {
                ConnectToLobby(await LobbyManager.QuickJoinLobby());
            }
        }

        public virtual async void JoinLobbySpecific(Lobby lobby)
        {
            if (await AbleToConnect())
            {
                ConnectToLobby(await LobbyManager.JoinLobby(lobby: lobby));
            }
        }

        public virtual async void CreateNewLobby(string roomName = null, bool isPrivate = false, int playerCount = maxPlayers)
        {
            if (await AbleToConnect())
            {
                ConnectToLobby(await LobbyManager.CreateLobby(roomName, isPrivate, playerCount));
            }
        }

        protected virtual async Task<bool> AbleToConnect()
        {
            if (connectionState.Value == ConnectionState.Connecting)
            {
                string failureMessage = "Connection attempt still in progress.";
                Utils.Log($"{debugPrepend}{failureMessage}", 1);
                ConnectionFailed(failureMessage);
                return false;
            }

            if (Connected.Value || connectionState.Value == ConnectionState.Connected)
            {
                Utils.Log($"{debugPrepend}Already Connected to a Lobby. Disconnecting.", 0);
                await DisconnectAsync();

                // Small wait while everything finishes disconnecting.
                await Task.Delay(100);
            }

            connectionState.Value = ConnectionState.Connecting;
            return true;
        }

        protected virtual void ConnectToLobby(Lobby lobby)
        {
            if (lobby == null || !ConnectedToLobby())
            {
                FailedToConnect();
            }
        }

        protected virtual bool ConnectedToLobby()
        {
            bool connected;

            if (LobbyManager.connectedLobby.HostId == AuthenicationId)
            {
                connected = NetworkManager.Singleton.StartHost();
            }
            else
            {
                connected = NetworkManager.Singleton.StartClient();
            }

            if (connected)
            {
                connectionState.Value = ConnectionState.Connected;
                SubscribeToLobbyEvents();
            }
            else
            {
                Utils.Log($"{debugPrepend}Failed to connect to lobby {LobbyManager.connectedLobby.Name}.");
                LobbyManager.OnLobbyFailed?.Invoke($"Failed to connect to lobby {LobbyManager.connectedLobby.Name}.");
            }

            return connected;
        }

        protected virtual async void SubscribeToLobbyEvents()
        {
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
            try
            {
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(LobbyManager.connectedLobby.Id, callbacks);
            }
            catch (LobbyServiceException ex)
            {
                switch (ex.Reason)
                {
                    case LobbyExceptionReason.AlreadySubscribedToLobby: Utils.Log($"{debugPrepend}Already subscribed to lobby[{LobbyManager.connectedLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}", 1); break;
                    case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Utils.Log($"{debugPrepend}Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}", 2); throw;
                    case LobbyExceptionReason.LobbyEventServiceConnectionError: Utils.Log($"{debugPrepend}Failed to connect to lobby events. Exception Message: {ex.Message}", 2); throw;
                    default: throw;
                }
            }
        }

        private void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
        {
            switch (state)
            {
                case LobbyEventConnectionState.Unsubscribed: Utils.Log($"{debugPrepend}Lobby event now Unsubscribed"); break;
                case LobbyEventConnectionState.Subscribing: Utils.Log($"{debugPrepend}Attempting to subscribe to lobby events"); break;
                case LobbyEventConnectionState.Subscribed: Utils.Log($"{debugPrepend}Subscribing to lobby events now"); break;
                case LobbyEventConnectionState.Unsynced:
                    LobbyManager.ReconnectToLobby();
                    Utils.Log($"{debugPrepend}Lobby Events now unsynced.\n\n{state}", 1);
                    break;
                case LobbyEventConnectionState.Error: Utils.Log($"{debugPrepend}Lobby event error.\n\n{state}", 2); break;
            }
        }

        protected virtual void OnLobbyChanged(ILobbyChanges changes)
        {
            if (!changes.LobbyDeleted)
            {
                changes.ApplyToLobby(LobbyManager.connectedLobby);
            }
        }

        protected virtual void FailedToConnect(string reason = null)
        {
            string failureMessage = "Failed to connect to lobby.";
            if (reason != null)
            {
                failureMessage = $"{reason}";
            }
            Utils.Log($"{debugPrepend}{failureMessage}", 1);
        }

        public virtual async void CancelMatchmaking()
        {
            if (IsAuthenticated())
            {
                connectionState.Value = ConnectionState.Authenticated;
            }
            await LobbyManager.RemovePlayerFromLobby(AuthenicationId);
        }

        public virtual async void Disconnect()
        {
            await DisconnectAsync();
        }

        public virtual async Task<bool> DisconnectAsync()
        {
            bool fullyDisconnected = await LobbyManager.RemovePlayerFromLobby(AuthenicationId);
            connected.Value = false;
            NetworkManager.Shutdown();
            if (IsAuthenticated())
            {
                connectionState.Value = ConnectionState.Authenticated;
            }
            else
            {
                connectionState.Value = ConnectionState.None;
            }
            return fullyDisconnected;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                networkedRoles.Clear();
                for (int i = 0; i < Enum.GetValues(typeof(Role)).Length; i++)
                {
                    networkedRoles.Add(new NetworkedRole { playerID = 0, isOccupied = false });
                }
            }

            UpdateNetworkedRoleVisuals();
            networkedRoles.OnListChanged += OnNetworkRolesChanged;

            if (IsServer)
                playerStateChanged += OnPlayerStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            foreach (var roleButton in roleButtons)
            {
                roleButton.RemovePlayerFromRole();
            }

            playerStateChanged -= OnPlayerStateChanged;
            networkedRoles.OnListChanged -= OnNetworkRolesChanged;
        }

        void OnNetworkRolesChanged(NetworkListEvent<NetworkedRole> changeEvent)
        {
            UpdateNetworkedRoleVisuals();
        }

        void OnPlayerStateChanged(ulong playerID, bool connected)
        {
            if (!connected)
            {
                for (int i = 0; i < networkedRoles.Count; i++)
                {
                    if (networkedRoles[i].playerID == playerID)
                    {
                        ServerRemoveRole(i);
                    }
                }
                UpdateNetworkedRoleVisuals();
            }
        }

        [Rpc(SendTo.Server)]
        public void RequestRoleServerRpc(int newRoleID, ulong newClientID)
        {
            if (networkedRoles[newRoleID].isOccupied)
            {
                Debug.LogError($"Role {newRoleID} is already occupied");
                return;
            }

            ServerAssignRole(newRoleID, newClientID);
        }

        void ServerAssignRole(int newRoleID, ulong localPlayerID)
        {
            networkedRoles[newRoleID] = new NetworkedRole { playerID = localPlayerID, isOccupied = true };

            UpdateNetworkedRoleVisuals();
            AssignRoleRpc(newRoleID, localPlayerID);
        }

        void ServerRemoveRole(int roleID)
        {
            networkedRoles[roleID] = new NetworkedRole { playerID = 0, isOccupied = false };
            UpdateNetworkedRoleVisuals();
            RemovePlayerFromRoleRpc(roleID);
        }

        [Rpc(SendTo.Everyone)]
        void RemovePlayerFromRoleRpc(int roleID)
        {
            roleButtons[roleID].RemovePlayerFromRole();
        }

        void UpdateNetworkedRoleVisuals()
        {
            for (int i = 0; i < networkedRoles.Count; i++)
            {
                if (!networkedRoles[i].isOccupied)
                {
                    roleButtons[i].SetOccupied(false);
                }
                else
                {
                    if (TryGetPlayerByID(networkedRoles[i].playerID, out var player))
                    {
                        roleButtons[i].AssignPlayerToRole(player);
                    }
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        void AssignRoleRpc(int roleID, ulong playerID)
        {
            if (TryGetPlayerByID(playerID, out var player))
            {
                roleButtons[roleID].AssignPlayerToRole(player);
            }
            else
            {
                Debug.Log($"Player with id {playerID} not found");
            }
        }
    }

    [Serializable]
    public struct NetworkedRole : INetworkSerializable, IEquatable<NetworkedRole>
    {
        public bool isOccupied;
        public ulong playerID;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref isOccupied);
            serializer.SerializeValue(ref playerID);
        }

        public readonly bool Equals(NetworkedRole other)
        {
            return isOccupied == other.isOccupied && playerID == other.playerID;
        }
    }

    public enum Role
    {
        ZED,
        ServerTracker,
        HMD,
        Tablet,
    }
}