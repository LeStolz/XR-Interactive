using System;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using XRMultiplayer;

public class NetworkSeatManager : NetworkBehaviour
{
    enum SeatType
    {
        Current = -3,
        Any = -2,
    }

    static int k_CurrentSeat = 0;

    [SerializeField]
    Seat[] m_Seats;

    [SerializeField]
    SeatButton[] m_SeatButtons;

    public NetworkList<NetworkedSeat> networkedSeats;

    XROrigin m_XROrigin;

    void Awake()
    {
        networkedSeats = new NetworkList<NetworkedSeat>();

        m_XROrigin = FindFirstObjectByType<XROrigin>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            networkedSeats.Clear();
            for (int i = 0; i < m_SeatButtons.Length; i++)
            {
                networkedSeats.Add(new NetworkedSeat { isOccupied = false, playerID = 0 });
            }
        }

        UpdateNetworkedSeatsVisuals();
        networkedSeats.OnListChanged += OnOccupiedSeatsChanged;
        RequestSeat();

        if (IsServer)
        {
            XRINetworkGameManager.Instance.playerStateChanged += OnPlayerStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        foreach (var seatButton in m_SeatButtons)
        {
            seatButton.RemovePlayerFromSeat();
        }
        networkedSeats.OnListChanged -= OnOccupiedSeatsChanged;
        XRINetworkGameManager.Instance.playerStateChanged -= OnPlayerStateChanged;
        TeleportToSeat(0);
        k_CurrentSeat = 0;
    }

    void OnOccupiedSeatsChanged(NetworkListEvent<NetworkedSeat> changeEvent)
    {
        UpdateNetworkedSeatsVisuals();
    }

    void OnPlayerStateChanged(ulong playerID, bool connected)
    {
        if (!connected)
        {
            for (int i = 0; i < networkedSeats.Count; i++)
            {
                if (networkedSeats[i].playerID == playerID)
                {
                    ServerRemovePlayerFromSeat(i);
                }
            }

            UpdateNetworkedSeatsVisuals();
        }
    }

    void UpdateNetworkedSeatsVisuals()
    {
        for (int i = 0; i < networkedSeats.Count; i++)
        {
            if (!networkedSeats[i].isOccupied)
            {
                m_SeatButtons[i].SetOccupied(false);
            }
            else
            {
                if (XRINetworkGameManager.Instance.TryGetPlayerByID(networkedSeats[i].playerID, out var player))
                {
                    m_SeatButtons[i].AssignPlayerToSeat(player);
                }
                else
                {
                    Debug.LogError($"Player with id {networkedSeats[i].playerID} not found");
                }
            }
        }
    }

    public void TeleportToSeat(int seatNum = (int)SeatType.Current)
    {
        if (seatNum == (int)SeatType.Current)
            seatNum = k_CurrentSeat;

        k_CurrentSeat = seatNum;

        var seat = m_Seats[k_CurrentSeat].seatTransform;

        if (m_XROrigin == null)
            m_XROrigin = FindFirstObjectByType<XROrigin>();

        seat.GetPositionAndRotation(out var seatGlobalPosition, out var seatGlobalRotation);
        m_XROrigin.transform.SetPositionAndRotation(seatGlobalPosition, seatGlobalRotation);
    }

    public void RequestSeat(int newSeatChoice = (int)SeatType.Any)
    {
        RequestSeatServerRpc(NetworkManager.Singleton.LocalClientId, k_CurrentSeat, newSeatChoice);
    }

    [Rpc(SendTo.Server)]
    void RequestSeatServerRpc(ulong localPlayerID, int currentSeatID, int newSeatID)
    {
        if (newSeatID == (int)SeatType.Any)
            newSeatID = GetAnyAvailableSeats();

        if (!networkedSeats[newSeatID].isOccupied)
            ServerAssignSeat(currentSeatID, newSeatID, localPlayerID);
        else
            Debug.Log("User tried to join an occupied seat");
    }

    int GetAnyAvailableSeats()
    {
        for (int i = 0; i < networkedSeats.Count; i++)
        {
            if (!networkedSeats[i].isOccupied)
            {
                return i;
            }
        }

        return 0;
    }

    void ServerAssignSeat(int currentSeatID, int newSeatID, ulong localPlayerID)
    {
        if (currentSeatID >= 0)
        {
            ServerRemovePlayerFromSeat(currentSeatID);
        }
        if (newSeatID >= 0)
        {
            networkedSeats[newSeatID] = new NetworkedSeat { isOccupied = true, playerID = localPlayerID };
        }

        UpdateNetworkedSeatsVisuals();

        AssignSeatRpc(newSeatID, localPlayerID);
    }

    void ServerRemovePlayerFromSeat(int seatID)
    {
        networkedSeats[seatID] = new NetworkedSeat { isOccupied = false, playerID = 0 };
        UpdateNetworkedSeatsVisuals();
        RemovePlayerFromSeatRpc(seatID);
    }

    [Rpc(SendTo.Everyone)]
    void RemovePlayerFromSeatRpc(int seatID)
    {
        m_SeatButtons[seatID].RemovePlayerFromSeat();
    }

    [Rpc(SendTo.Everyone)]
    void AssignSeatRpc(int seatID, ulong playerID)
    {
        if (XRINetworkGameManager.Instance.TryGetPlayerByID(playerID, out var player))
        {
            m_SeatButtons[seatID].AssignPlayerToSeat(player);
            if (playerID == NetworkManager.Singleton.LocalClientId)
            {
                TeleportToSeat(seatID);
            }
        }
        else
        {
            Debug.LogError($"Player with id {playerID} not found");
        }
    }
}

[Serializable]
public struct NetworkedSeat : INetworkSerializable, IEquatable<NetworkedSeat>
{
    public bool isOccupied;
    public ulong playerID;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref isOccupied);
        serializer.SerializeValue(ref playerID);
    }

    public readonly bool Equals(NetworkedSeat other)
    {
        return isOccupied == other.isOccupied && playerID == other.playerID;
    }
}

[Serializable]
public struct Seat
{
    public Transform seatTransform;
    public int seatID;
}