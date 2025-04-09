using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Main
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
            NetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
        }

        void Start()
        {
            SetPlayerName(NetworkGameManager.LocalPlayerName.Value);

            if (NetworkGameManager.Instance.localRole != Role.Tablet)
            {
                gameObject.SetActive(false);
                confirmButton.onClick.Invoke();
                SubmitNewPlayerName(NetworkGameManager.Instance.localRole.ToString());
            }
        }

        void OnDestroy()
        {
            NetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
        }

        /// <summary>
        /// Use this to set the player's name so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SubmitNewPlayerName(string text)
        {
            NetworkGameManager.LocalPlayerName.Value = text;
        }

        void SetPlayerName(string newName)
        {
            m_PlayerNameInputField.text = newName;
        }
    }
}
