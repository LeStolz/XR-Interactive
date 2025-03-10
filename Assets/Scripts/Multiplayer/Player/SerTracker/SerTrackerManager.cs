
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    class SerTrackerManager : NetworkPlayer
    {
        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        new GameObject camera;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                foreach (var obj in objectsToEnableOnSpawn)
                {
                    if (obj.TryGetComponent(out Camera camera))
                    {
                        camera.enabled = true;
                    }
                    else
                    {
                        obj.SetActive(true);
                    }
                }

                var ZedModelManager = FindFirstObjectByType<ZedModelManager>();
                if (ZedModelManager != null)
                {
                    ZedModelManager.RequestCalibrationRpc();
                }
            }
        }

        [Rpc(SendTo.Owner)]
        public void CalibrateRpc(Vector3 position, Vector3 rotation)
        {
            transform.SetPositionAndRotation(
                position - camera.transform.position,
                Quaternion.Euler(rotation - camera.transform.eulerAngles)
            );
        }
    }
}