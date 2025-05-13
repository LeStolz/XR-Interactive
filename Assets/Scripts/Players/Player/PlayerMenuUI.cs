using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace Main
{
    [DefaultExecutionOrder(100)]
    public class PlayerMenuUI : MonoBehaviour
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

        [Header("Player Options")]
        [SerializeField] Vector2 m_MinMaxMoveSpeed = new Vector2(2.0f, 10.0f);
        [SerializeField] Vector2 m_MinMaxTurnAmount = new Vector2(15.0f, 180.0f);
        [SerializeField] float m_SnapTurnUpdateAmount = 15.0f;

        [SerializeField]
        GameObject[] onlineHMDUIs;
        [SerializeField]
        GameObject[] offlineHMDUIs;
        [SerializeField]
        GameObject[] onlineServerTrackerUIs;
        [SerializeField]
        GameObject[] offlineZEDUIs;

        [SerializeField] GameObject calibrationPrefab;

        int m_CurrentPanel = 0;
        DynamicMoveProvider m_MoveProvider;
        SnapTurnProvider m_TurnProvider;

        private void Awake()
        {
            m_MoveProvider = FindAnyObjectByType<DynamicMoveProvider>();
            m_TurnProvider = FindAnyObjectByType<SnapTurnProvider>();

            NetworkGameManager.Connected.Subscribe(ConnectOnline);

            HideCurrentUI();

            ToggleControlPanel(false);
        }

        void OnEnable()
        {
            // Enable and disable directly serialized actions with this behavior's enabled lifecycle.
            m_MenuButtonInput.EnableDirectActionIfModeUsed();

            TogglePanel(NetworkGameManager.Connected.Value ? 0 : m_DefaultPanel);
        }

        void OnDisable()
        {
            m_MenuButtonInput.DisableDirectActionIfModeUsed();
        }

        private void OnDestroy()
        {
            NetworkGameManager.Connected.Unsubscribe(ConnectOnline);
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

        void ToggleControlPanel(bool connected)
        {
            void Toggle(GameObject[] gos, bool on)
            {
                foreach (var go in gos)
                {
                    go.SetActive(on);
                }
            }

            if (connected)
            {
                switch (NetworkGameManager.Instance.localRole)
                {
                    case Role.HMD:
                        Toggle(onlineHMDUIs, true);
                        Toggle(offlineHMDUIs, false);
                        break;
                    case Role.ServerTracker:
                        Toggle(onlineServerTrackerUIs, true);

                        if (onlineServerTrackerUIs[0].TryGetComponent(out TrackerUI trackerUI))
                            trackerUI.SetTrackers();
                        break;
                    case Role.ZED:
                        Toggle(offlineZEDUIs, false);
                        break;
                    case Role.Tablet:
                        break;
                }
            }
            else
            {
                switch (NetworkGameManager.Instance.localRole)
                {
                    case Role.HMD:
                        Toggle(onlineHMDUIs, false);
                        Toggle(offlineHMDUIs, true);

                        if (calibrationPrefab != null)
                        {
                            var recalibration = Instantiate(calibrationPrefab, Vector3.zero, Quaternion.identity);

                            if (offlineHMDUIs[0].TryGetComponent(out Toggle toggle))
                            {
                                toggle.onValueChanged.AddListener((_) =>
                                    recalibration.GetComponentInChildren<CalibrationManager>().Recalibrate()
                                );
                            }

                            calibrationPrefab = null;
                        }
                        break;
                    case Role.ServerTracker:
                        Toggle(onlineServerTrackerUIs, false);
                        break;
                    case Role.ZED:
                        Toggle(offlineZEDUIs, true);
                        break;
                    case Role.Tablet:
                        break;
                }
            }
        }

        void ConnectOnline(bool connected)
        {
            ToggleControlPanel(connected);

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
            TogglePanel(NetworkGameManager.Connected.Value ? 0 : m_DefaultPanel);
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
            NetworkGameManager.Instance.Disconnect();
            TogglePanel(m_DefaultPanel);
        }

        public void QuickJoin()
        {
            NetworkGameManager.Instance.CreateLobby();
        }

        public void SetVolumeLevel(float sliderValue)
        {
            m_Mixer.SetFloat("MainVolume", Mathf.Log10(sliderValue) * 20);
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
            if (!NetworkGameManager.Connected.Value)
            {
                TogglePanel(2);
            }
        }
    }
}
