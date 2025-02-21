using UnityEngine;
using TMPro;
using UnityEngine.UI;
using XRMultiplayer;

public class RoleButton : MonoBehaviour
{
    public Color[] seatColors => m_SeatColors;

    [SerializeField]
    Color[] m_SeatColors;

    [SerializeField]
    Image[] m_SeatImages;

    [SerializeField]
    TMP_Text m_PlayerInSeatText;

    [SerializeField]
    GameObject m_SeatUnoccupiedObject;

    [SerializeField]
    GameObject m_SeatOccupiedObject;

    [SerializeField]
    GameObject m_OwnedSeatUI;

    [SerializeField]
    GameObject m_TakenSeatUI;

    [SerializeField, Range(0, 3)]
    int m_SeatID;

    [SerializeField]
    bool m_IsOccupied = false;

    [SerializeField]
    string m_AvailableSeatText = "<color=#7B7B7B><i>Available</i></color>";

    string m_PlayerNameInSeat = "Player Name";

    XRINetworkPlayer m_PlayerInSeat;

    void OnValidate()
    {
        foreach (var icon in m_SeatImages)
            icon.color = m_SeatColors[m_SeatID];

        SetOccupied(m_IsOccupied);
    }

    public void SetPlayerName(string name)
    {
        m_PlayerNameInSeat = name;
        m_PlayerInSeatText.text = m_IsOccupied ? m_PlayerNameInSeat : m_AvailableSeatText;
    }

    public void AssignPlayerToSeat(XRINetworkPlayer player)
    {
        if (m_PlayerInSeat != null)
            RemovePlayerFromSeat();

        m_PlayerInSeat = player;

        SetPlayerName(m_PlayerInSeat.playerName);

        m_PlayerInSeat.onNameUpdated += SetPlayerName;

        if (m_PlayerInSeat.IsLocalPlayer)
            XRINetworkGameManager.LocalPlayerColor.Value = m_SeatColors[m_SeatID];

        SetOccupied(true);
    }

    public void RemovePlayerFromSeat()
    {
        if (m_PlayerInSeat == null)
        {
            Debug.LogWarning("Trying to remove player from seat but no player is assigned to this seat.");
            return;
        }
        m_PlayerInSeat.onNameUpdated -= SetPlayerName;

        m_PlayerInSeat = null;
        SetOccupied(false);
    }

    public void SetOccupied(bool occupied)
    {
        m_IsOccupied = occupied;
        m_SeatImages[1].enabled = m_IsOccupied;
        m_PlayerInSeatText.text = m_IsOccupied ? m_PlayerNameInSeat : m_AvailableSeatText;
    }
}
