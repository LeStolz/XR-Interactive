using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

namespace XRMultiplayer
{
    public class PlayerSlot : MonoBehaviour
    {
        public TMP_Text playerSlotName;
        public TMP_Text playerInitial;
        public Image playerIconImage;

        [Header("Mic Button")]
        public Image voiceChatFillImage;
        [SerializeField] Button m_MicButton;
        [SerializeField] Image m_PlayerVoiceIcon;
        [SerializeField] Image m_SquelchedIcon;
        [SerializeField] Sprite[] micIcons;
        XRINetworkPlayer m_Player;
        internal ulong playerID = 0;

        public void Setup(XRINetworkPlayer player)
        {
            m_Player = player;
            m_Player.onNameUpdated += UpdateName;
            m_SquelchedIcon.enabled = false;
            if (m_Player.IsLocalPlayer)
            {
                m_MicButton.interactable = false;
            }
        }

        void OnDestroy()
        {
            m_Player.onNameUpdated -= UpdateName;
        }

        void UpdateColor(Color newColor)
        {
            playerIconImage.color = newColor;
        }

        void UpdateName(string newName)
        {
            if (!newName.IsNullOrEmpty())
            {
                string playerName = newName;
                if (m_Player.IsLocalPlayer)
                {
                    playerName += " (You)";
                }
                else if (m_Player.IsOwnedByServer)
                {
                    playerName += " (Host)";
                }
                playerSlotName.text = playerName;
                playerInitial.text = newName.Substring(0, 1);
            }
        }
    }
}
