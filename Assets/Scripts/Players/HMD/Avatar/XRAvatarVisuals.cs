using System;
using UnityEngine;

namespace Main
{
    public class XRAvatarVisuals : MonoBehaviour
    {
        /// <summary>
        /// Head Renderers to change rendering mode for local players.
        /// </summary>
        [Header("Renderer References"), SerializeField, Tooltip("Head Renderers to change rendering mode for local players.")]
        protected Renderer[] m_HeadRends;

        /// <summary>
        /// Materials to swap for the local player.
        /// </summary>
        [Header("Local Player Material Swap"), SerializeField]
        protected LocalPlayerMaterialSwap m_LocalPlayerMaterialSwap;

        /// <summary>
        /// Reference to the attached XRINetworkPlayer component.
        /// </summary>
        [SerializeField]
        protected NetworkPlayer m_NetworkPlayer;

        public virtual void Awake()
        {
            m_NetworkPlayer.onSpawnedLocal += PlayerSpawnedLocal;
        }

        public virtual void OnDestroy()
        {
            m_NetworkPlayer.onSpawnedLocal -= PlayerSpawnedLocal;
        }

        public virtual void PlayerSpawnedLocal()
        {
            m_LocalPlayerMaterialSwap.SwapMaterials();
            Debug.Log("Player spawned locally. Swapping materials and setting layer to Mirror.");
            int layer = LayerMask.NameToLayer("Mirror");
            foreach (var r in m_HeadRends)
            {
                r.gameObject.layer = layer;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
    }
}

[Serializable]

/// <summary>
/// Helper class for swapping the local player to standard materials from the dithering materials.
/// </summary>
public class LocalPlayerMaterialSwap
{
    public Renderer headRend;
    public Renderer hmdRend;
    public Renderer hostRend;
    public Renderer[] hands;
    public Material[] headMaterials;
    public Material[] hmdMaterials;
    public Material hostMaterial;
    public Material handMaterial;


    public void SwapMaterials()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            hands[i].material = handMaterial;
        }

        hmdRend.materials = hmdMaterials;
        headRend.materials = headMaterials;
        hostRend.material = hostMaterial;
    }
}
