using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Main
{
    public class XRINetworkAvatar : NetworkPlayer
    {
        [SerializeField] private readonly float marginX = 0.5f * 2.5f;
        [SerializeField] private readonly float marginY = 0.6f * 2.5f;

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

        public static bool IsOutOfView(Transform target, float marginX, float marginY)
        {
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(target.position);
            bool isOutOfView = viewportPoint.x < -marginX || viewportPoint.x > 1 + marginX ||
                           viewportPoint.y < -marginY || viewportPoint.y > 1 + marginY ||
                           viewportPoint.z < 0;
            return isOutOfView;
        }

        private void ToggleHand(Transform networkedHand, Transform originHand)
        {
            if (handSubsystem == null)
            {
                originHand.parent.parent.GetComponentInChildren<NearFarInteractor>().enabled = false;
                return;
            }

            var hand = networkedHand.gameObject.name.ToLower().Contains("left")
                    ? handSubsystem.leftHand
                    : handSubsystem.rightHand;

            if (!hand.isTracked)
            {
                originHand.parent.parent.GetComponentInChildren<NearFarInteractor>().enabled = false;
                return;
            }

            originHand.parent.parent.GetComponentInChildren<NearFarInteractor>().enabled = true;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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
