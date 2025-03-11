
using Unity.Netcode;
using UnityEngine;
using Valve.VR;

namespace Multiplayer
{
    class SerTrackerManager : NetworkPlayer
    {
        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        new GameObject camera;
        bool calibrating = false;

        Vector3 position;
        Vector3 rotation;

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

        void LateUpdate()
        {
            if (!calibrating)
            {
                return;
            }

            transform.SetPositionAndRotation(
                position - camera.transform.position,
                Quaternion.Euler(rotation - camera.transform.eulerAngles)
            );

            camera.GetComponent<SteamVR_TrackedObject>().enabled = false;
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            calibrating = false;
        }

        [Rpc(SendTo.Owner)]
        public void CalibrateRpc(Vector3 position, Vector3 rotation)
        {
            calibrating = true;
            camera.GetComponent<SteamVR_TrackedObject>().enabled = true;
            this.position = position;
            this.rotation = rotation;
        }
    }
}