using UnityEngine;
using TMPro;
using UnityEngine.UI;
using XRMultiplayer;

public class SeatButton : MonoBehaviour
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

    [SerializeField]
    GameObject m_HoveredObject;

    [Header("Editor Variables")]
    [SerializeField]
    bool m_IsSpectator = false;

    [SerializeField, Range(0, 3)]
    int m_SeatID;

    [SerializeField]
    bool m_IsHovered = false;

    [SerializeField]
    bool m_IsOccupied = false;

    [SerializeField]
    bool m_IsLocalPlayer = false;

    [SerializeField]
    string m_AvailableSeatText = "<color=#7B7B7B><i>Available</i></color>";

    string m_PlayerNameInSeat = "Player Name";

    XRINetworkPlayer m_PlayerInSeat;

    void OnValidate()
    {
        if (!m_IsSpectator)
        {
            m_SeatID = Mathf.Clamp(m_SeatID, 0, m_SeatColors.Length - 1);
            foreach (var icon in m_SeatImages)
                icon.color = m_SeatColors[m_SeatID];
        }
        else
        {
            // Spectator is always true because we remove the player from the list if nt
            m_IsOccupied = true;
        }

        SetOccupied(m_IsOccupied);
    }

    public void SetPlayerName(string name)
    {
        m_PlayerNameInSeat = name;
        m_PlayerInSeatText.text = m_IsOccupied ? (m_IsLocalPlayer ? "You" : m_PlayerNameInSeat) : m_AvailableSeatText;
    }

    public void SetLocalPlayer(bool local, bool updateOccupied = true)
    {
        m_IsLocalPlayer = local;
        if (updateOccupied)
            SetOccupied(m_IsOccupied);
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

        SetLocalPlayer(m_PlayerInSeat.IsLocalPlayer, false);
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
        SetLocalPlayer(false);
        SetOccupied(false);
    }

    public void SetOccupied(bool occupied)
    {
        m_IsOccupied = occupied;
        if (!m_IsSpectator)
            m_SeatImages[1].enabled = m_IsOccupied;
        m_PlayerInSeatText.text = m_IsOccupied ? (m_IsLocalPlayer ? "You" : m_PlayerNameInSeat) : m_AvailableSeatText;

        SetHover(m_IsHovered);
    }

    public void SetHover(bool hover)
    {
        m_IsHovered = hover;
        if (!m_IsHovered)
        {
            m_HoveredObject.SetActive(false);

            if (!m_IsSpectator)
                m_PlayerInSeatText.gameObject.SetActive(true);
            return;
        }

        m_HoveredObject.SetActive(true);

        if (!m_IsSpectator)
            m_PlayerInSeatText.gameObject.SetActive(false);

        if (m_IsOccupied)
        {
            m_SeatUnoccupiedObject.SetActive(false);
            m_SeatOccupiedObject.SetActive(true);
            m_OwnedSeatUI.SetActive(m_IsLocalPlayer);
            m_TakenSeatUI.SetActive(!m_IsLocalPlayer);
        }
        else
        {
            m_SeatUnoccupiedObject.SetActive(true);
            m_SeatOccupiedObject.SetActive(false);
        }
    }
}
