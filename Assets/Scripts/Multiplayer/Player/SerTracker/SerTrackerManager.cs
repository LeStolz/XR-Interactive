
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
        [SerializeField]
        TrackerManager trackerManager;
        public bool IsLocalOwner => IsOwner;

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

                var ZedModelManager = FindFirstObjectByType<ZEDModelManager>();
                if (ZedModelManager != null)
                {
                    ZedModelManager.RequestCalibrationRpc();
                }
            }
        }

        [Rpc(SendTo.Owner)]
        public void CalibrateRpc(
            Vector3 eulerPos, Vector3 targetForward,
            int cameraPixelWidth, int cameraPixelHeight
        )
        {
            transform.rotation = Quaternion.identity;

            var targetForwardXZ = new Vector3(targetForward.x, 0, targetForward.z).normalized;
            var forwardXZ = new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z).normalized;
            var angleDifference = Vector3.SignedAngle(forwardXZ, targetForwardXZ, Vector3.up);

            transform.Rotate(Vector3.up, angleDifference);
            transform.position = eulerPos - (camera.transform.position - transform.position);

            var ZEDModelManager = FindFirstObjectByType<ZEDModelManager>();
            if (ZEDModelManager != null)
            {
                trackerManager.SetOutputPortal(
                    ZEDModelManager.OutputPortal.gameObject,
                    cameraPixelWidth, cameraPixelHeight
                );
            }
        }

        [Rpc(SendTo.Everyone)]
        public void DrawLineRpc(int hitMarkerId, Vector3 start, Vector3 end)
        {
            trackerManager.DrawLine(hitMarkerId, start, end);
        }
    }
}