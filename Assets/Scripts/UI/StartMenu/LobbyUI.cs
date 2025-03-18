using System.Collections;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;

namespace Multiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Lobby List")]
        [SerializeField] Transform m_LobbyListParent;
        [SerializeField] GameObject m_LobbyListPrefab;
        [SerializeField] float m_AutoRefreshTime = 5.0f;
        [SerializeField] float m_RefreshCooldownTime = .5f;

        [Header("Connection Texts")]
        [SerializeField] TMP_Text m_ConnectionUpdatedText;
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;

        [SerializeField] TMP_InputField m_RoomNameText;

        [SerializeField] GameObject[] m_ConnectionSubPanels;

        Coroutine m_UpdateLobbiesRoutine;
        Coroutine m_CooldownFillRoutine;

        int m_PlayerCount;

        private void Awake()
        {
            LobbyManager.status.Subscribe(ConnectedUpdated);
        }

        private void Start()
        {
            m_PlayerCount = NetworkGameManager.maxPlayers;

            NetworkGameManager.Instance.connectionFailedAction += FailedToConnect;
            NetworkGameManager.Instance.connectionUpdated += ConnectedUpdated;

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }
        }

        private void OnEnable()
        {
            CheckInternetAsync();
        }

        private void OnDisable()
        {
            HideLobbies();
        }

        private void OnDestroy()
        {
            NetworkGameManager.Instance.connectionFailedAction -= FailedToConnect;
            NetworkGameManager.Instance.connectionUpdated -= ConnectedUpdated;

            LobbyManager.status.Unsubscribe(ConnectedUpdated);
        }
        public async void CheckInternetAsync()
        {
            if (NetworkGameManager.Instance == null)
                return;

            if (!NetworkGameManager.Instance.IsAuthenticated())
            {
                try
                {
                    await NetworkGameManager.Instance.Authenticate();
                    ToggleConnectionSubPanel(4);
                }
                catch
                {
                    ToggleConnectionSubPanel(0);
                    return;
                }
            }
            CheckForInternet();
        }

        void CheckForInternet()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ToggleConnectionSubPanel(4);
            }
            else
            {
                ToggleConnectionSubPanel(0);
            }
        }

        public void CreateLobby()
        {
            NetworkGameManager.Connected.Subscribe(OnConnected);
            m_RoomNameText.text = $"{NetworkGameManager.LocalPlayerName.Value}'s Table";
            NetworkGameManager.Instance.CreateNewLobby(m_RoomNameText.text, false, m_PlayerCount);
            m_ConnectionSuccessText.text = $"Joining {m_RoomNameText.text}";
        }

        public void UpdatePlayerCount(int count)
        {
            m_PlayerCount = Mathf.Clamp(count, 1, NetworkGameManager.maxPlayers);
        }

        public void CancelConnection()
        {
            NetworkGameManager.Instance.CancelMatchmaking();
        }

        public void JoinLobby(Lobby lobby)
        {
            ToggleConnectionSubPanel(1);
            NetworkGameManager.Connected.Subscribe(OnConnected);
            NetworkGameManager.Instance.JoinLobbySpecific(lobby);
            m_ConnectionSuccessText.text = $"Joining {lobby.Name}";
        }

        public void QuickJoinLobby()
        {
            NetworkGameManager.Connected.Subscribe(OnConnected);
            NetworkGameManager.Instance.QuickJoinLobby();
            m_ConnectionSuccessText.text = "Joining Random";
        }

        public void ToggleConnectionSubPanel(int panelId)
        {
            for (int i = 0; i < m_ConnectionSubPanels.Length; i++)
            {
                m_ConnectionSubPanels[i].SetActive(i == panelId);
            }


            if (panelId == 1)
            {
                ShowLobbies();
            }
            else
            {
                HideLobbies();
            }
        }

        void OnConnected(bool connected)
        {
            if (connected)
            {
                ToggleConnectionSubPanel(2);

                // Unsubscribe from the event after connection to prevent multiple subscriptions
                NetworkGameManager.Connected.Unsubscribe(OnConnected);
            }
        }

        void ConnectedUpdated(string update)
        {
            m_ConnectionUpdatedText.text = $"<b>Status:</b> {update}";
        }

        public void FailedToConnect(string reason)
        {
            ToggleConnectionSubPanel(3);
            m_ConnectionFailedText.text = $"<b>Error:</b> {reason}";
        }

        public void HideLobbies()
        {
            EnableRefresh();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
        }

        public void ShowLobbies()
        {
            GetAllLobbies();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
            m_UpdateLobbiesRoutine = StartCoroutine(UpdateAvailableLobbies());
        }

        IEnumerator UpdateAvailableLobbies()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_AutoRefreshTime);
                GetAllLobbies();
            }
        }

        bool onCD = false;
        void EnableRefresh()
        {
            onCD = false;
        }

        IEnumerator UpdateButtonCooldown()
        {
            onCD = true;
            yield return new WaitForSeconds(m_RefreshCooldownTime);
            EnableRefresh();
        }

        async void GetAllLobbies()
        {
            if (onCD || (int)NetworkGameManager.CurrentConnectionState.Value < 2) return;
            if (m_CooldownFillRoutine != null) StopCoroutine(m_CooldownFillRoutine);
            m_CooldownFillRoutine = StartCoroutine(UpdateButtonCooldown());

            QueryResponse lobbies = await LobbyManager.GetLobbiesAsync();

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }

            if (lobbies.Results != null || lobbies.Results.Count > 0)
            {
                foreach (var lobby in lobbies.Results)
                {
                    if (LobbyManager.CheckForLobbyFilter(lobby))
                    {
                        continue;
                    }

                    if (LobbyManager.CheckForIncompatibilityFilter(lobby))
                    {
                        LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, m_LobbyListParent).GetComponent<LobbyListSlotUI>();
                        newLobbyUI.CreateNonJoinableLobbyUI(lobby, this, "Version Conflict");
                        continue;
                    }

                    if (LobbyManager.CanJoinLobby(lobby))
                    {
                        LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, m_LobbyListParent).GetComponent<LobbyListSlotUI>();
                        newLobbyUI.CreateLobbyUI(lobby, this);
                    }
                }
            }
        }
    }
}
