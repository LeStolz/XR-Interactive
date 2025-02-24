using TMPro;
using UnityEngine;

namespace Multiplayer
{
    /// <summary>
    /// A simple example of how to setup a player appearance menu and utilize the bindable variables.
    /// </summary>
    public class PlayerNameMenu : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_PlayerNameInputField;

        void Awake()
        {
            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
        }

        void Start()
        {
            SetPlayerName(XRINetworkGameManager.LocalPlayerName.Value);
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
