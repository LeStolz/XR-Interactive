using System;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using XRMultiplayer;

public class NetworkRoleManager : NetworkBehaviour
{
    public enum Role
    {
        ZEDTracker,
        Server,
        HMD,
        Tablet,
    }

    [SerializeField]
    RoleButton[] roleButtons;

    public Role role;
    public NetworkList<NetworkedRole> networkedRoles;

    void Awake()
    {
        networkedRoles = new NetworkList<NetworkedRole>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            networkedRoles.Clear();
            for (int i = 0; i < Enum.GetValues(typeof(Role)).Length; i++)
            {
                networkedRoles.Add(new NetworkedRole { playerID = 0 });
            }
            XRINetworkGameManager.Instance.playerStateChanged += OnPlayerStateChanged;
        }

        RequestRoleServerRpc(NetworkManager.Singleton.LocalClientId, (int)role);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        XRINetworkGameManager.Instance.playerStateChanged -= OnPlayerStateChanged;

        TeleportToRoleDefault(false, role);
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
        if (networkedRoles[newRoleID].playerID != 0)
        {
            Debug.LogError($"Role {newRoleID} is already occupied");
            return;
        }

        ServerAssignRole(newRoleID, localPlayerID);
    }

    void ServerAssignRole(int newRoleID, ulong localPlayerID)
    {
        if (newRoleID >= 0)
        {
            networkedRoles[newRoleID] = new NetworkedRole { playerID = localPlayerID };
        }

        UpdateNetworkedRoleVisuals();

        AssignRoleRpc(newRoleID, localPlayerID);
    }

    void ServerRemoveRole(int roleID)
    {
        networkedRoles[roleID] = new NetworkedRole { playerID = 0 };

        UpdateNetworkedRoleVisuals();
        UpdateNetworkedRoleVisualsRpc();
    }

    [Rpc(SendTo.Everyone)]
    void UpdateNetworkedRoleVisualsRpc()
    {
        UpdateNetworkedRoleVisuals();
    }

    void UpdateNetworkedRoleVisuals()
    {
        for (int i = 0; i < roleButtons.Length; i++)
        {
            if (networkedRoles[i].playerID == 0)
            {
                roleButtons[i].SetOccupied(false);
            }
            else
            {
                if (XRINetworkGameManager.Instance.TryGetPlayerByID(networkedRoles[i].playerID, out var player))
                    roleButtons[i].AssignPlayerToSeat(player);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    void AssignRoleRpc(int roleID, ulong playerID)
    {
        if (XRINetworkGameManager.Instance.TryGetPlayerByID(playerID, out var player))
        {
            if (playerID == NetworkManager.Singleton.LocalClientId)
            {
                TeleportToRoleDefault(true, (Role)roleID);
            }
        }
        else
        {
            Debug.LogError($"Player with id {playerID} not found");
        }
    }
}

[Serializable]
public struct NetworkedRole : INetworkSerializable, IEquatable<NetworkedRole>
{
    public ulong playerID;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerID);
    }

    public readonly bool Equals(NetworkedRole other)
    {
        return playerID == other.playerID;
    }
}