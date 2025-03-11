
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
            var targetRot = Quaternion.Euler(rotation);
            Vector3 pivot = transform.position;
            Vector3 currentEuler = transform.rotation.eulerAngles;
            var rotationDiff = targetRot * Quaternion.Inverse(transform.rotation);

            transform.RotateAround(pivot, Vector3.right, rotationDiff.eulerAngles.x - currentEuler.x);
            transform.RotateAround(pivot, Vector3.up, rotationDiff.eulerAngles.y - currentEuler.y);
            transform.RotateAround(pivot, Vector3.forward, rotationDiff.eulerAngles.z - currentEuler.z);

            transform.position = position - camera.transform.localPosition;
        }
    }
}