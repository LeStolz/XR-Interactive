using System.Collections;
using UnityEngine;
using TMPro;

namespace Multiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Lobby List")]
        [SerializeField] Transform m_LobbyListParent;
        [SerializeField] GameObject m_LobbyListPrefab;
        [SerializeField] float getLobbiesTime = 5.0f;
        [SerializeField] float refreshLobbiesTime = .5f;

        [Header("Connection Texts")]
        [SerializeField] TMP_Text m_ConnectionUpdatedText;
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;

        [SerializeField] GameObject[] m_ConnectionSubPanels;

        Coroutine GetLobbiesRoutine;
        Coroutine RefreshLobbiesRoutine;

        private void Awake()
        {
            LobbyManager.status.Subscribe(ConnectedUpdated);
        }

        private void Start()
        {
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
        public void CheckInternetAsync()
        {
            if (NetworkGameManager.Instance == null)
                return;

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

        public void JoinLobby(DiscoveryResponseData lobby)
        {
            ToggleConnectionSubPanel(1);
            NetworkGameManager.Connected.Subscribe(OnConnected);
            NetworkGameManager.Instance.JoinLobbySpecific(lobby);
            m_ConnectionSuccessText.text = $"Joining {lobby.ServerName}";
        }

        public void CreateLobby()
        {
            NetworkGameManager.Connected.Subscribe(OnConnected);
            NetworkGameManager.Instance.CreateLobby();
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
            if (GetLobbiesRoutine != null) StopCoroutine(GetLobbiesRoutine);
            if (RefreshLobbiesRoutine != null) StopCoroutine(RefreshLobbiesRoutine);
        }

        public void ShowLobbies()
        {
            GetAllLobbies();
            if (GetLobbiesRoutine != null) StopCoroutine(GetLobbiesRoutine);
            GetLobbiesRoutine = StartCoroutine(GetAvailableLobbies());

            if (RefreshLobbiesRoutine != null) StopCoroutine(RefreshLobbiesRoutine);
            RefreshLobbiesRoutine = StartCoroutine(RefreshAvailableLobbies());
        }

        IEnumerator GetAvailableLobbies()
        {
            while (true)
            {
                yield return new WaitForSeconds(getLobbiesTime);
                GetAllLobbies();
            }
        }

        IEnumerator RefreshAvailableLobbies()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshLobbiesTime);
                LobbyManager.RefreshLobbies();
            }
        }

        void GetAllLobbies()
        {
            if ((int)NetworkGameManager.CurrentConnectionState.Value < 2) return;

            DiscoveryResponseData[] lobbies = LobbyManager.GetLobbies();

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }

            if (lobbies != null || lobbies.Length > 0)
            {
                foreach (var lobby in lobbies)
                {
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
