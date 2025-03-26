using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using System.Collections;
using UnityEngine.XR.Management;
using System.Linq;

namespace Multiplayer
{
#if USE_FORCED_BYTE_SERIALIZATION
    class ForceByteSerialization : NetworkBehaviour
    {
        NetworkVariable<byte> m_ForceByteSerialization;
    }
#endif

    [RequireComponent(typeof(LobbyManager))]
    public class NetworkGameManager : NetworkBehaviour
    {
        public enum ConnectionState
        {
            Offline,
            Connecting,
            Connected
        }

        public static NetworkGameManager Instance { get; private set; }
        public static ulong LocalId;

        public static BindableVariable<string> LocalPlayerName = new("");
        public static IReadOnlyBindableVariable<bool> Connected
        {
            get => connected;
        }
        static BindableVariable<bool> connected = new BindableVariable<bool>(false);
        static BindableEnum<ConnectionState> connectionState = new BindableEnum<ConnectionState>(ConnectionState.Offline);

        public Action<ulong, bool> playerStateChanged;
        public Action<string> connectionUpdated;
        public Action<string> connectionFailedAction;

        public LobbyManager LobbyManager { get; private set; }


        readonly List<PlayerData> currentPlayers = new();


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
        RoleUI[] roleButtons;
        public Role localRole;

        bool isShuttingDown = false;
        const string debugPrepend = "<color=#FAC00C>[Network Game Manager]</color> ";

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LocalPlayerName.Value = "";

            if (TryGetComponent(out LobbyManager lobbyManager))
            {
                LobbyManager = lobbyManager;
                LobbyManager.OnLobbyFailed += ConnectionFailed;
            }
            else
            {
                enabled = false;
                return;
            }

            connected.Value = false;
            connectionState.Value = ConnectionState.Offline;
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

        void ShutDown()
        {
            if (isShuttingDown) return;
            isShuttingDown = true;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientStopped -= OnLocalClientStopped;
            }
        }

        protected virtual void OnLocalClientStopped(bool id)
        {
            connected.Value = false;
            currentPlayers.Clear();
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Disconnected");
            connectionState.Value = ConnectionState.Offline;
        }

        /// <summary>
        /// This function will set the player ID in the list <see cref="currentPlayers"/> and
        /// invokes the callback <see cref="playerStateChanged"/>.
        /// </summary>
        /// <param name="id"><see cref="NetworkObject.OwnerClientId"/> of the joined player.</param>
        /// <remarks>Called from <see cref="NetworkPlayer.CompleteSetup"/>.</remarks>
        public virtual void PlayerJoined(ulong id, NetworkPlayer player)
        {
            if (!currentPlayers.Any(playerData => playerData.id == id))
            {
                Role role = GetRoleFromPlayer(player);
                currentPlayers.Add(
                    new PlayerData
                    {
                        id = id,
                        player = player,
                        role = role
                    }
                );
                playerStateChanged?.Invoke(id, true);

                roleButtons[(int)role].AssignPlayerToRole(player);

                UpdateNetworkedRoleVisuals();
            }
            else
            {
                Utils.Log($"{debugPrepend}Trying to Add a player ID [{id}] that already exists", 1);
            }

            if (id == NetworkManager.Singleton.LocalClientId)
            {
                LocalId = id;
                connected.Value = true;
                PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Connected");
            }
        }

        /// <summary>
        /// Called from <see cref="NetworkPlayer.OnDestroy"/>.
        /// </summary>
        /// <param name="id"><see cref="NetworkObject.OwnerClientId"/> of the player who left.</param>
        public virtual void PlayerLeft(ulong id)
        {
            if (currentPlayers.Any(playerData => playerData.id == id))
            {
                var player = currentPlayers.Find(playerData => playerData.id == id);
                currentPlayers.Remove(player);
                playerStateChanged?.Invoke(id, false);

                roleButtons[(int)player.role].RemovePlayerFromRole();

                UpdateNetworkedRoleVisuals();

                if (id == NetworkManager.Singleton.LocalClientId)
                {
                    foreach (var roleButton in roleButtons)
                    {
                        roleButton.RemovePlayerFromRole();
                    }
                }
            }
            else
            {
                Utils.Log($"{debugPrepend}Trying to remove a player ID [{id}] that doesn't exist", 1);
            }
        }

        void UpdateNetworkedRoleVisuals()
        {
            foreach (var roleButton in roleButtons)
            {
                roleButton.SetOccupied(false);
            }

            foreach (var player in currentPlayers)
            {
                roleButtons[(int)player.role].AssignPlayerToRole(player.player);
            }
        }

        Role GetRoleFromPlayer(NetworkPlayer player)
        {
            if (player is ZEDModelManager)
            {
                return Role.ZED;
            }
            else if (player is ServerTrackerManager)
            {
                return Role.ServerTracker;
            }
            else if (player is TabletManager)
            {
                return Role.Tablet;
            }

            return Role.HMD;
        }

        public T FindPlayerByRole<T>(Role role)
        {
            var player = currentPlayers.Find(playerData => playerData.role == role).player;

            if (player == null)
            {
                return default;
            }

            return player.GetComponent<T>();
        }

        public virtual bool TryGetPlayerByID(ulong id, out NetworkPlayer player)
        {
            player = currentPlayers.Find(playerData => playerData.id == id).player;

            return player != null;
        }


        public virtual void ConnectionFailed(string reason)
        {
            connectionFailedAction?.Invoke(reason);
            connectionState.Value = ConnectionState.Offline;
        }

        public virtual void ConnectionUpdated(string update)
        {
            connectionUpdated?.Invoke(update);
        }

        public virtual async void CreateLobby()
        {
            if (await AbleToConnect())
            {
                LobbyManager.CreateLobby();
                ConnectToLobby();
            }
        }

        public virtual async void JoinLobbySpecific(DiscoveryResponseData lobby)
        {
            if (await AbleToConnect())
            {
                LobbyManager.JoinLobby(lobby: lobby);
                ConnectToLobby();
            }
        }

        public virtual async void CreateNewLobby()
        {
            if (await AbleToConnect())
            {
                LobbyManager.CreateLobby();
                ConnectToLobby();
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
                Disconnect();

                // Small wait while everything finishes disconnecting.
                await Task.Delay(100);
            }

            connectionState.Value = ConnectionState.Connecting;
            return true;
        }

        protected virtual void ConnectToLobby()
        {
            connectionState.Value = ConnectionState.Connected;
            Utils.Log($"{debugPrepend}Connected to lobby.");
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

        public virtual void Disconnect()
        {
            connected.Value = false;

            LobbyManager.Instance.StartLobbyDiscovery();

            NetworkManager.Singleton.Shutdown();
            connectionState.Value = ConnectionState.Offline;
        }
    }

    public struct PlayerData
    {
        public Role role;
        public ulong id;
        public NetworkPlayer player;

        public readonly bool Equals(PlayerData other)
        {
            return id == other.id && player == other.player && role == other.role;
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