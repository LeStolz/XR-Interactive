using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Multiplayer
{
    public class LobbyListSlotUI : MonoBehaviour
    {
        [SerializeField] TMP_Text m_RoomNameText;
        [SerializeField] TMP_Text m_PlayerCountText;
        [SerializeField] Button m_JoinButton;
        [SerializeField] GameObject m_FullImage;
        [SerializeField] TMP_Text m_StatusText;
        [SerializeField] GameObject m_JoinImage;

        LobbyUI m_LobbyListUI;
        DiscoveryResponseData m_Lobby;

        bool m_NonJoinable = false;

        void OnDisable()
        {
            ToggleHover(false);
        }

        public void CreateLobbyUI(DiscoveryResponseData lobby, LobbyUI lobbyListUI)
        {
            m_NonJoinable = false;
            m_Lobby = lobby;
            m_LobbyListUI = lobbyListUI;
            m_JoinButton.onClick.AddListener(JoinRoom);
            m_RoomNameText.text = lobby.ServerName;
            m_PlayerCountText.text = "";
            m_JoinButton.interactable = true;
            m_FullImage.SetActive(false);
            m_JoinImage.SetActive(false);
        }

        public void ToggleHover(bool toggle)
        {
            if (m_NonJoinable) return;
            if (toggle)
            {
                SetLobbyAvailable(true);
            }
            else
            {
                m_FullImage.SetActive(false);
                m_JoinImage.SetActive(false);
            }
        }

        void SetLobbyAvailable(bool available)
        {
            m_JoinImage.SetActive(available);
            m_FullImage.SetActive(!available);

            m_JoinButton.interactable = available;
        }

        private void OnDestroy()
        {
            m_JoinButton.onClick.RemoveListener(JoinRoom);
        }

        void JoinRoom()
        {
            m_LobbyListUI.JoinLobby(m_Lobby);
        }
    }
}
