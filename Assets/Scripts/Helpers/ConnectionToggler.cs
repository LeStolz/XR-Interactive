using UnityEngine;

namespace Main
{
    /// <summary>
    /// A very simple script that will enable or disable objects based on the Network Connection State.
    /// </summary>
    public class ConnectionToggler : MonoBehaviour
    {
        /// <summary>
        /// Enables all objects on connect.
        /// Disables all objects on disconnect.
        /// </summary>
        [SerializeField] GameObject[] objectsToEnableOnline;

        /// <summary>
        /// Enables all objects on disconnect.
        /// Disables all objects on connect.
        /// </summary>
        [SerializeField] GameObject[] objectsToEnableOffline;

        /// <inheritdoc/>
        void OnEnable()
        {
            NetworkGameManager.Connected.Subscribe(SetObjectsActive);
            SetObjectsActive(NetworkGameManager.Connected.Value);
        }

        void Start()
        {
            NetworkGameManager.Instance.connectionFailedAction += (reason) =>
            {
                SetObjectsActive(false);
            };
        }

        void OnDestroy()
        {
            NetworkGameManager.Instance.connectionFailedAction -= (reason) =>
            {
                SetObjectsActive(false);
            };
        }

        /// <inheritdoc/>
        void OnDisable()
        {
            NetworkGameManager.Connected.Unsubscribe(SetObjectsActive);
        }

        /// <summary>
        /// Toggles objects on or off based on whether or not connected.
        /// <see cref="m_Connected"/>
        /// </summary>
        /// <param name="online">
        /// Whether or not players are connected to a networked game.
        /// </param>
        public void SetObjectsActive(bool online)
        {
            foreach (GameObject g in objectsToEnableOnline)
            {
                if (g == null) continue;
                g.SetActive(online);
            }

            foreach (GameObject g in objectsToEnableOffline)
            {
                if (g == null) continue;
                g.SetActive(!online);
            }
        }

        public void ToggleOnlineObjects()
        {
            if (NetworkGameManager.Connected.Value)
            {
                foreach (GameObject g in objectsToEnableOnline)
                {
                    if (g == null) continue;
                    g.SetActive(!g.activeInHierarchy);
                }
            }
        }
    }
}
