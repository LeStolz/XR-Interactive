using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    class ServerTrackerManager : NetworkPlayer
    {
        [SerializeField]
        Vector3 OFFSET;

        [field: SerializeField]
        public Tracker[] Trackers { get; private set; }
        [SerializeField]
        new GameObject camera;
        [SerializeField]
        GameObject canvas;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                canvas.SetActive(false);
            }

            Calibrate(0);
        }

        public void Calibrate(int trackerId)
        {
            if (trackerId < 0 || trackerId >= Trackers.Length)
            {
                return;
            }

            var trackerParent = Trackers[trackerId].transform.parent;
            var thisParent = transform.parent;

            Trackers[trackerId].transform.SetParent(null);
            transform.SetParent(Trackers[trackerId].transform);

            Trackers[trackerId].transform.SetPositionAndRotation(OFFSET, Quaternion.Euler(new(-90, 0, 0)));

            transform.SetParent(thisParent);
            Trackers[trackerId].transform.SetParent(trackerParent);

            CalibrateRpc(transform.position, transform.rotation.eulerAngles);
        }

        [Rpc(SendTo.NotMe)]
        void CalibrateRpc(Vector3 position, Vector3 rotation)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }
    }
}