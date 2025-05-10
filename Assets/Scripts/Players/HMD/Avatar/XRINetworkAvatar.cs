using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Main
{
    public class XRINetworkAvatar : NetworkPlayer
    {
        [SerializeField] private float marginX = 0.5f * 1.5f;
        [SerializeField] private float marginY = 0.6f * 1.5f;

        [Header("Avatar Transform References"), Tooltip("Assign to local avatar transform.")]
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        [Header("Networked Hands"), SerializeField, Tooltip("Hand Objects to be disabled for the local player.")]
        protected GameObject[] m_handsObjects;
        protected Transform m_LeftHandOrigin, m_RightHandOrigin, m_HeadOrigin;
        [SerializeField] protected XROrigin m_XROrigin;
        protected Vector3 m_PrevHeadPos;

        XRHandSubsystem handSubsystem;

        void Start()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
            {
                handSubsystem = subsystems[0];
            }
        }

        ///<inheritdoc/>
        protected virtual void LateUpdate()
        {
            if (!IsOwner) return;

            ToggleHand(leftHand, m_LeftHandOrigin);
            ToggleHand(rightHand, m_RightHandOrigin);

            leftHand.SetPositionAndRotation(m_LeftHandOrigin.position, m_LeftHandOrigin.rotation);
            rightHand.SetPositionAndRotation(m_RightHandOrigin.position, m_RightHandOrigin.rotation);
            head.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        private void ToggleHand(Transform networkedHand, Transform originHand)
        {
            bool isLeftHand = networkedHand.gameObject.name.ToLower().Contains("left");

            if (
                handSubsystem != null &&
                (
                    isLeftHand && !handSubsystem.leftHand.isTracked ||
                    !isLeftHand && !handSubsystem.rightHand.isTracked
                ) &&
                networkedHand.position == originHand.position
            )
            {
                originHand.parent.parent.GetComponentInChildren<NearFarInteractor>().enabled = false;
                return;
            }

            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(originHand.position);
            bool isOutOfView = viewportPoint.x < -marginX || viewportPoint.x > 1 + marginX ||
                       viewportPoint.y < -marginY || viewportPoint.y > 1 + marginY ||
                       viewportPoint.z < 0;

            originHand.parent.parent.GetComponentInChildren<NearFarInteractor>().enabled = !isOutOfView;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            SetupPlayer();
        }

        private void SetupPlayer()
        {
            if (IsOwner)
            {
                NetworkGameManager.Instance.TableUI.SetActive(false);

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

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                NetworkGameManager.Instance.TableUI.SetActive(true);
            }
        }

        public void SetHandOrigins(Transform left, Transform right)
        {
            m_LeftHandOrigin = left;
            m_RightHandOrigin = right;
        }

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
