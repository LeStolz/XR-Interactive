using System;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;

public enum Role
{
    ZED,
    SerTracker,
    HMD,
    Tablet,
}

namespace Multiplayer
{
    class NetworkRoleManager : NetworkBehaviour
    {
        [field: SerializeField]
        public GameObject MRInteractionSetup { get; private set; }
        [SerializeField]
        Canvas WorldSpaceCanvas;
        [SerializeField]
        Canvas ScreenSpaceCanvas;
        [SerializeField]
        public GameObject TableUI { get; private set; }
        [SerializeField]
        int tableScale;

        [SerializeField]
        RoleButton[] roleButtons;

        public static NetworkRoleManager Instance { get; private set; }
        public Role localRole;
        NetworkList<NetworkedRole> networkedRoles;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            networkedRoles = new NetworkList<NetworkedRole>();
        }

        void Start()
        {
            if (localRole != Role.HMD)
            {
                TableUI.transform.parent = ScreenSpaceCanvas.transform;
                TableUI.transform.localScale = new Vector3(tableScale, tableScale, tableScale);
                TableUI.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
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
            RequestRoleServerRpc(NetworkManager.Singleton.LocalClientId, (int)localRole);

            if (IsServer)
                XRINetworkGameManager.Instance.playerStateChanged += OnPlayerStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            foreach (var roleButton in roleButtons)
            {
                roleButton.RemovePlayerFromRole();
            }

            XRINetworkGameManager.Instance.playerStateChanged -= OnPlayerStateChanged;
            networkedRoles.OnListChanged -= OnNetworkRolesChanged;

            TeleportToRoleDefault(false, localRole);
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

        public void TeleportToRoleDefault(bool online, Role role)
        {
            if (!online)
            {
                var XROrigin = FindFirstObjectByType<XROrigin>();
                if (XROrigin != null)
                {
                    XROrigin.transform.SetPositionAndRotation(new(0, 0.4f, -0.6f), Quaternion.identity);
                }

                return;
            }

            // Teleport to role's default position
        }

        [Rpc(SendTo.Server)]
        void RequestRoleServerRpc(ulong localPlayerID, int newRoleID)
        {
            if (networkedRoles[newRoleID].isOccupied)
            {
                Debug.LogError($"Role {newRoleID} is already occupied");
                return;
            }

            ServerAssignRole(newRoleID, localPlayerID);
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
                    if (XRINetworkGameManager.Instance.TryGetPlayerByID(networkedRoles[i].playerID, out var player))
                    {
                        roleButtons[i].AssignPlayerToRole(player);
                    }
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        void AssignRoleRpc(int roleID, ulong playerID)
        {
            if (XRINetworkGameManager.Instance.TryGetPlayerByID(playerID, out var player))
            {
                roleButtons[roleID].AssignPlayerToRole(player);
                if (playerID == NetworkManager.Singleton.LocalClientId)
                {
                    TeleportToRoleDefault(true, (Role)roleID);
                }
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
}