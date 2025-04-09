using Unity.XR.CoreUtils;
using UnityEngine;

namespace Main
{
    /// <summary>
    /// Represents the offline player avatar.
    /// </summary>
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        /// <summary>
        /// The head transform.
        /// </summary>
        [SerializeField] Transform m_HeadTransform;

        /// <summary>
        /// The head origin.
        /// </summary>
        Transform m_HeadOrigin;

        /// <inheritdoc/>
        void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            m_HeadOrigin = rig.Camera.transform;
        }

        void OnEnable()
        {
            NetworkGameManager.Connected.Subscribe((bool connected) => gameObject.SetActive(!connected));
        }

        void OnDisable()
        {
            NetworkGameManager.Connected.Unsubscribe((bool connected) => gameObject.SetActive(!connected));
        }

        /// <inheritdoc/>
        private void LateUpdate()
        {
            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }
    }
}
