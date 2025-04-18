using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Main
{
    public class LobbyListSlotUI : MonoBehaviour
    {
        [SerializeField] TMP_Text m_RoomNameText;
        [SerializeField] Button m_JoinButton;
        [SerializeField] GameObject m_FullImage;
        [SerializeField] TMP_Text m_StatusText;
        [SerializeField] GameObject m_JoinImage;

        LobbyUI m_LobbyListUI;
        DiscoveryResponseData m_Lobby;

        void OnDisable()
        {
            ToggleHover(false);
        }

        public void CreateLobbyUI(DiscoveryResponseData lobby, LobbyUI lobbyListUI)
        {
            m_Lobby = lobby;
            m_LobbyListUI = lobbyListUI;
            m_JoinButton.onClick.AddListener(JoinRoom);
            m_RoomNameText.text = lobby.ServerName;
            m_JoinButton.interactable = true;
            m_FullImage.SetActive(false);
            m_JoinImage.SetActive(false);
        }

        public void ToggleHover(bool toggle)
        {
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
