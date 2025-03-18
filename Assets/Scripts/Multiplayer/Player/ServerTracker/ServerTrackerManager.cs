using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    class ServerTrackerManager : NetworkPlayer
    {
        [field: SerializeField]
        public Tracker[] Trackers { get; private set; }
        [SerializeField]
        new GameObject camera;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                foreach (var tracker in Trackers)
                {
                    tracker.gameObject.SetActive(true);
                }

                var ZedModelManager = FindFirstObjectByType<ZEDModelManager>();
                if (ZedModelManager != null)
                {
                    ZedModelManager.RequestCalibrationRpc();
                }
            }
        }

        [Rpc(SendTo.Owner)]
        public void CalibrateRpc(Vector3 eulerPos, Vector3 targetForward)
        {
            transform.rotation = Quaternion.identity;

            var targetForwardXZ = new Vector3(targetForward.x, 0, targetForward.z).normalized;
            var forwardXZ = new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z).normalized;
            var angleDifference = Vector3.SignedAngle(forwardXZ, targetForwardXZ, Vector3.up);

            transform.Rotate(Vector3.up, angleDifference);
            transform.position = eulerPos - (camera.transform.position - transform.position);
        }

        [Rpc(SendTo.Everyone)]
        public void DrawLineRpc(int trackerId, int hitMarkerId, Vector3 start, Vector3 end)
        {
            Trackers[trackerId].DrawLine(hitMarkerId, start, end);
        }
    }
}