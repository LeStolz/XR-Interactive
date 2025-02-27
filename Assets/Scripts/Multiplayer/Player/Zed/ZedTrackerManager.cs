
using UnityEngine;

namespace Multiplayer
{
    class ZedTrackerManager : NetworkPlayer
    {
        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        GameObject zedModel;
        [SerializeField]
        GameObject cameraEyes;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                NetworkRoleManager.Instance.MRInteractionSetup.SetActive(false);

                foreach (var obj in objectsToEnableOnSpawn)
                {
                    obj.SetActive(true);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                NetworkRoleManager.Instance.MRInteractionSetup.SetActive(true);
            }
        }

        void Update()
        {
            if (IsOwner)
            {
                zedModel.transform.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);
            }
        }
    }
}