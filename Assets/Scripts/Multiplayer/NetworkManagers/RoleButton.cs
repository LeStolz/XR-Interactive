using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Multiplayer
{
    class RoleButton : MonoBehaviour
    {
        public Color[] RoleColors => m_RoleColors;

        [SerializeField]
        Color[] m_RoleColors;

        [SerializeField]
        Image[] m_RoleImages;

        [SerializeField]
        TMP_Text m_PlayerInRoleText;

        [SerializeField]
        GameObject m_RoleUnoccupiedObject;

        [SerializeField]
        GameObject m_RoleOccupiedObject;

        [SerializeField]
        GameObject m_OwnedRoleUI;

        [SerializeField]
        GameObject m_TakenRoleUI;

        [SerializeField, Range(0, 3)]
        int m_RoleID;

        [SerializeField]
        bool m_IsOccupied = false;

        [SerializeField]
        string m_AvailableRoleText = "<color=#7B7B7B><i>Available</i></color>";

        string m_PlayerNameInRole = "Player Name";

        NetworkPlayer m_PlayerInRole;

        void OnValidate()
        {
            foreach (var icon in m_RoleImages)
                icon.color = m_RoleColors[m_RoleID];

            SetOccupied(m_IsOccupied);
        }

        public void SetPlayerName(string name)
        {
            m_PlayerNameInRole = name;
            m_PlayerInRoleText.text = m_IsOccupied ? m_PlayerNameInRole : m_AvailableRoleText;
        }

        public void AssignPlayerToRole(NetworkPlayer player)
        {
            if (m_PlayerInRole != null)
                RemovePlayerFromRole();

            m_PlayerInRole = player;

            SetPlayerName(m_PlayerInRole.PlayerName);

            m_PlayerInRole.onNameUpdated += SetPlayerName;

            SetOccupied(true);
        }

        public void RemovePlayerFromRole()
        {
            if (m_PlayerInRole == null)
            {
                Debug.LogWarning("Trying to remove player from role but no player is assigned to this role.");
                return;
            }
            m_PlayerInRole.onNameUpdated -= SetPlayerName;

            m_PlayerInRole = null;
            SetOccupied(false);
        }

        public void SetOccupied(bool occupied)
        {
            m_IsOccupied = occupied;
            m_RoleImages[1].enabled = m_IsOccupied;
            m_PlayerInRoleText.text = m_IsOccupied ? m_PlayerNameInRole : m_AvailableRoleText;
        }
    }
}