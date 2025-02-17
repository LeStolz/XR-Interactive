using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.Android;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace XRMultiplayer
{
    [DefaultExecutionOrder(100)]
    public class PlayerMenu : MonoBehaviour
    {
        [SerializeField]
        XRInputButtonReader m_MenuButtonInput = new XRInputButtonReader("Menu Button");
        public XRInputButtonReader menuInput
        {
            get => m_MenuButtonInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_MenuButtonInput, value, this);
        }
        [SerializeField] AudioMixer m_Mixer;
        [SerializeField] int m_DefaultPanel = 1;
        [SerializeField] UnityEvent<bool> m_OnMenuToggle;

        [Header("Panels")]
        [SerializeField] GameObject m_MainPanelRoot;
        [SerializeField] GameObject m_HostRoomPanel;
        [SerializeField] GameObject m_ClientRoomPanel;
        [SerializeField] GameObject m_LeaveTableButtonObject;
        [SerializeField] GameObject[] m_OfflineWarningPanels;
        [SerializeField] GameObject[] m_OnlinePanels;
        [SerializeField] GameObject[] m_Panels;
        [SerializeField] Toggle[] m_PanelToggles;
        [SerializeField] GameObject m_AppearanceConfirmButton;
        [SerializeField] GameObject m_LeaveRoomPanel;

        [Header("Text Components")]
        [SerializeField] TMP_Text[] m_RoomCodeTexts;
        [SerializeField] TMP_Text[] m_RoomNameText;
        [SerializeField] TMP_InputField m_RoomNameInputField;

        [Header("Player Options")]
        [SerializeField] Vector2 m_MinMaxMoveSpeed = new Vector2(2.0f, 10.0f);
        [SerializeField] Vector2 m_MinMaxTurnAmount = new Vector2(15.0f, 180.0f);
        [SerializeField] float m_SnapTurnUpdateAmount = 15.0f;

        int m_CurrentPanel = 0;
        DynamicMoveProvider m_MoveProvider;
        SnapTurnProvider m_TurnProvider;

        private void Awake()
        {
            m_MoveProvider = FindAnyObjectByType<DynamicMoveProvider>();
            m_TurnProvider = FindAnyObjectByType<SnapTurnProvider>();

            XRINetworkGameManager.Connected.Subscribe(ConnectOnline);
            XRINetworkGameManager.ConnectedRoomName.Subscribe(UpdateRoomName);

            // ConnectOnline(false);
            HideCurrentUI();
        }

        void OnEnable()
        {
            // Enable and disable directly serialized actions with this behavior's enabled lifecycle.
            m_MenuButtonInput.EnableDirectActionIfModeUsed();

            TogglePanel(XRINetworkGameManager.Connected.Value ? 0 : m_DefaultPanel);
        }

        void OnDisable()
        {
            m_MenuButtonInput.DisableDirectActionIfModeUsed();
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(ConnectOnline);
            XRINetworkGameManager.ConnectedRoomName.Unsubscribe(UpdateRoomName);
        }

        private void Update()
        {
            if (m_MenuButtonInput != null)
            {
                if (m_MenuButtonInput.ReadWasPerformedThisFrame())
                {
                    ToggleMenu();
                }
            }
            if (XRINetworkGameManager.Connected.Value)
            {
            }
            else
            {
            }
        }

        void HideCurrentUI()
        {
            foreach (var go in m_OfflineWarningPanels)
            {
                go.SetActive(false);
            }

            foreach (var go in m_OnlinePanels)
            {
                go.SetActive(false);
            }

            // m_AppearanceConfirmButton.SetActive(false);
        }

        void ConnectOnline(bool connected)
        {
            foreach (var go in m_OfflineWarningPanels)
            {
                go.SetActive(!connected);
            }

            foreach (var go in m_OnlinePanels)
            {
                go.SetActive(connected);
            }

            m_AppearanceConfirmButton.SetActive(!connected);

            if (connected)
            {
                m_HostRoomPanel.SetActive(NetworkManager.Singleton.IsServer);
                m_ClientRoomPanel.SetActive(!NetworkManager.Singleton.IsServer);
                UpdateRoomName(XRINetworkGameManager.ConnectedRoomName.Value);
                TogglePanel(0);
            }
            else
            {
                if (m_CurrentPanel == 0)
                {
                    TogglePanel(m_DefaultPanel);
                }
            }
        }

        public void HideAllPanels()
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_Panels[i].SetActive(false);
            }
        }

        public void TogglePanel(int panelID)
        {
            m_CurrentPanel = panelID;
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_PanelToggles[i].SetIsOnWithoutNotify(panelID == i);
                m_Panels[i].SetActive(i == panelID);
            }

            if (panelID == 0)
                m_LeaveTableButtonObject.SetActive(true);
        }

        /// <summary>
        /// Toggles the menu on or off.
        /// </summary>
        /// <param name="overrideToggle"></param>
        /// <param name="overrideValue"></param>
        public void ToggleMenu(bool overrideToggle = false, bool overrideValue = false)
        {
            if (overrideToggle)
            {
                m_MainPanelRoot.SetActive(overrideValue);
            }
            else
            {
                ToggleMenu();
            }
            TogglePanel(XRINetworkGameManager.Connected.Value ? 0 : m_DefaultPanel);
        }

        public void ToggleMenu()
        {
            m_MainPanelRoot.SetActive(!m_MainPanelRoot.activeSelf);
            m_OnMenuToggle.Invoke(m_MainPanelRoot.activeSelf);
        }

        public void SetMenuActive(bool active)
        {
            bool wasActive = m_MainPanelRoot.activeSelf;
            m_MainPanelRoot.SetActive(active);

            if (wasActive != active)
            {
                m_OnMenuToggle.Invoke(active);
            }
        }

        public void LeaveTableConfirmationPanel()
        {
            HideAllPanels();
            m_LeaveRoomPanel.SetActive(true);
        }

        public void LogOut()
        {
            XRINetworkGameManager.Instance.Disconnect();
            TogglePanel(m_DefaultPanel);
        }

        public void QuickJoin()
        {
            XRINetworkGameManager.Instance.QuickJoinLobby();
        }

        public void SetVolumeLevel(float sliderValue)
        {
            m_Mixer.SetFloat("MainVolume", Mathf.Log10(sliderValue) * 20);
        }

        // Room Options
        void UpdateRoomName(string newValue)
        {
            foreach (var text in m_RoomCodeTexts)
            {
                text.text = $"Table Code: {XRINetworkGameManager.ConnectedRoomCode}";
            }
            foreach (var t in m_RoomNameText)
            {
                t.text = XRINetworkGameManager.ConnectedRoomName.Value;
            }
            m_RoomNameInputField.text = XRINetworkGameManager.ConnectedRoomName.Value;
        }

        // Player Options
        public void SetHandOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
            }
        }
        public void SetHeadOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
            }
        }
        public void SetMoveSpeed(float speedPercent)
        {
            m_MoveProvider.moveSpeed = Mathf.Lerp(m_MinMaxMoveSpeed.x, m_MinMaxMoveSpeed.y, speedPercent);
        }

        public void UpdateSnapTurn(int dir)
        {
            float newTurnAmount = Mathf.Clamp(m_TurnProvider.turnAmount + (m_SnapTurnUpdateAmount * dir), m_MinMaxTurnAmount.x, m_MinMaxTurnAmount.y);
            m_TurnProvider.turnAmount = newTurnAmount;
        }

        public void ToggleFlight(bool toggle)
        {
            m_MoveProvider.useGravity = !toggle;
            m_MoveProvider.enableFly = toggle;
        }

        public void ConfirmAppearance()
        {
            if (!XRINetworkGameManager.Connected.Value)
            {
                TogglePanel(2);
            }
        }
    }
}
