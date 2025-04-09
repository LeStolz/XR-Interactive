using UnityEngine;
using Unity.XR.CoreUtils;

namespace Main
{
    public class XRINetworkAvatar : NetworkPlayer
    {
        [Header("Avatar Transform References"), Tooltip("Assign to local avatar transform.")]
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        [Header("Networked Hands"), SerializeField, Tooltip("Hand Objects to be disabled for the local player.")]
        protected GameObject[] m_handsObjects;
        protected Transform m_LeftHandOrigin, m_RightHandOrigin, m_HeadOrigin;
        protected XROrigin m_XROrigin;
        protected Vector3 m_PrevHeadPos;

        ///<inheritdoc/>
        protected virtual void LateUpdate()
        {
            if (!IsOwner) return;

            // Set transforms to be replicated with ClientNetworkTransforms
            leftHand.SetPositionAndRotation(m_LeftHandOrigin.position, m_LeftHandOrigin.rotation);
            rightHand.SetPositionAndRotation(m_RightHandOrigin.position, m_RightHandOrigin.rotation);
            head.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                m_XROrigin = FindFirstObjectByType<XROrigin>();
                if (m_XROrigin != null)
                {
                    m_HeadOrigin = m_XROrigin.Camera.transform;
                }
                else
                {
                    Utils.Log("No XR Rig Available", 1);
                }

                SetupLocalPlayer();
            }
        }

        /// <summary>
        /// Called from <see cref="XRHandPoseReplicator"/> when swapping between hand tracking and controllers.
        /// </summary>
        /// <param name="left">Transform for Left Hand.</param>
        /// <param name="right">Transform for Right Hand.</param>
        public void SetHandOrigins(Transform left, Transform right)
        {
            m_LeftHandOrigin = left;
            m_RightHandOrigin = right;
        }

        /// <summary>
        /// Hides and disables Renderers and GameObjects on the Local Player.
        /// Also sets the initial values for <see cref="m_PlayerColor"/> and <see cref="m_PlayerName"/>.
        /// Finally we subscribe to any updates for Color and Name.
        /// </summary>
        /// <remarks>Only called on the Local Player.</remarks>
        protected override void SetupLocalPlayer()
        {
            base.SetupLocalPlayer();

            foreach (var hand in m_handsObjects)
            {
                hand.SetActive(false);
            }
        }
    }
}
