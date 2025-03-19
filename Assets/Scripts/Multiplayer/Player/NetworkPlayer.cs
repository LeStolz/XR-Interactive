using Unity.Netcode;
using Unity.Collections;
using System;
using UnityEngine;
using System.Collections;

namespace Multiplayer
{
    public class NetworkPlayer : NetworkBehaviour
    {
        public static NetworkPlayer LocalPlayer;

        public Action<string> onNameUpdated;
        public Action onSpawnedLocal;
        public Action onSpawnedAll;
        public Action<NetworkPlayer> onDisconnected;

        public string PlayerName { get => m_PlayerName.Value.ToString(); }
        readonly NetworkVariable<FixedString128Bytes> m_PlayerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected bool m_UpdateObjectName = true;
        protected bool m_InitialConnected = false;

        ///<inheritdoc/>
        protected virtual void OnEnable()
        {
            m_PlayerName.OnValueChanged += UpdatePlayerName;
        }

        ///<inheritdoc/>
        protected virtual void OnDisable()
        {
            m_PlayerName.OnValueChanged -= UpdatePlayerName;
        }

        ///<inheritdoc/>
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (IsOwner)
            {
                NetworkGameManager.LocalPlayerName.Unsubscribe(UpdateLocalPlayerName);
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkGameManager.Instance.PlayerLeft(NetworkObject.OwnerClientId);
            }
        }

        ///<inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                LocalPlayer = this;
                NetworkGameManager.Instance.LocalPlayerConnected(NetworkObject.OwnerClientId);

                SetupLocalPlayer();
            }
            StartCoroutine(CompleteSetup());
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (PlayerHudNotification.Instance.gameObject.activeInHierarchy)
                PlayerHudNotification.Instance.ShowText($"<b>{m_PlayerName.Value}</b> left");
            onDisconnected?.Invoke(this);
        }

        /// <summary>
        /// Hides and disables Renderers and GameObjects on the Local Player.
        /// Also sets the initial values for <see cref="m_PlayerColor"/> and <see cref="m_PlayerName"/>.
        /// Finally we subscribe to any updates for Color and Name.
        /// </summary>
        /// <remarks>Only called on the Local Player.</remarks>
        protected virtual void SetupLocalPlayer()
        {
            m_PlayerName.Value = new FixedString128Bytes(NetworkGameManager.LocalPlayerName.Value);
            NetworkGameManager.LocalPlayerName.Subscribe(UpdateLocalPlayerName);

            onSpawnedLocal?.Invoke();
        }

        /// <summary>
        /// Callback for the bindable variable <see cref="NetworkGameManager.LocalPlayerName"/>.
        /// </summary>
        /// <param name="name">New Name for player.</param>
        /// <remarks>Only called on Local Player.</remarks>
        protected virtual void UpdateLocalPlayerName(string name)
        {
            m_PlayerName.Value = new FixedString128Bytes(NetworkGameManager.LocalPlayerName.Value);
        }

        /// <summary>
        /// Called when the player object is finished being setup.
        /// </summary>
        IEnumerator CompleteSetup()
        {
            NetworkGameManager.Instance.PlayerJoined(NetworkObject.OwnerClientId);

            UpdatePlayerName(new FixedString128Bytes(""), m_PlayerName.Value);

            yield return new WaitUntil(() => NetworkGameManager.Instance.IsSpawned);

            if (IsOwner)
            {
                NetworkGameManager.Instance.RequestRoleServerRpc(
                    (int)NetworkGameManager.Instance.localRole,
                    NetworkManager.Singleton.LocalClientId
                );
            }
            onSpawnedAll?.Invoke();
        }

        /// <summary>
        /// Callback anytime the local player sets <see cref="m_PlayerName"/>.
        /// </summary><remarks>Invokes the callback <see cref="onNameUpdated"/>.</remarks>
        void UpdatePlayerName(FixedString128Bytes oldName, FixedString128Bytes currentName)
        {
            onNameUpdated?.Invoke(currentName.ToString());

            if (!m_InitialConnected & !string.IsNullOrEmpty(currentName.ToString()))
            {
                m_InitialConnected = true;
                if (!IsLocalPlayer && PlayerHudNotification.Instance.gameObject.activeInHierarchy)
                    PlayerHudNotification.Instance.ShowText($"<b>{PlayerName}</b> joined");
            }

            if (m_UpdateObjectName)
                gameObject.name = currentName.ToString();
        }
    }
}
