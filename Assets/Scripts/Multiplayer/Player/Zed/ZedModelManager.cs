
using UnityEngine;

namespace Multiplayer
{
    class ZedModelManager : NetworkPlayer
    {
        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        GameObject zedModel;
        [SerializeField]
        GameObject cameraEyes;
        [SerializeField]
        GameObject display;
        [SerializeField]
        GameObject marker;
        [SerializeField]
        Transform leftEye;

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

                NetworkRoleManager.Instance.TableUI.SetActive(false);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                NetworkRoleManager.Instance.MRInteractionSetup.SetActive(true);
                NetworkRoleManager.Instance.TableUI.SetActive(true);
            }
        }

        void Update()
        {
            if (IsOwner)
            {
                if (marker.activeSelf)
                {
                    cameraEyes.transform.SetPositionAndRotation(leftEye.transform.position, leftEye.transform.rotation);
                    cameraEyes.transform.SetParent(marker.transform);

                    marker.transform.position = Vector3.zero;
                    marker.transform.eulerAngles = new Vector3(-90, 180, 0);
                    leftEye.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);
                    zedModel.transform.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);
                    display.transform.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);
                }
            }
        }
    }
}