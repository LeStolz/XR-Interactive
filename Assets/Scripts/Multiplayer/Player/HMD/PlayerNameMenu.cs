using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Multiplayer
{
    /// <summary>
    /// A simple example of how to setup a player appearance menu and utilize the bindable variables.
    /// </summary>
    public class PlayerNameMenu : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_PlayerNameInputField;
        [SerializeField] Button confirmButton;

        void Awake()
        {
            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
        }

        void Start()
        {
            SetPlayerName(XRINetworkGameManager.LocalPlayerName.Value);

            if (NetworkRoleManager.Instance.localRole != Role.Tablet)
            {
                gameObject.SetActive(false);
                confirmButton.onClick.Invoke();
            }

            SubmitNewPlayerName(NetworkRoleManager.Instance.localRole.ToString());
        }

        void OnDestroy()
        {
            XRINetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
        }

        /// <summary>
        /// Use this to set the player's name so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SubmitNewPlayerName(string text)
        {
            XRINetworkGameManager.LocalPlayerName.Value = text;
        }

        void SetPlayerName(string newName)
        {
            m_PlayerNameInputField.text = newName;
        }
    }
}
