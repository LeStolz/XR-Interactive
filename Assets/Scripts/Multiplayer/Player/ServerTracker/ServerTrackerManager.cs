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

            Trackers[trackerId].transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new(-90, 0, 0)));

            transform.SetParent(thisParent);
            Trackers[trackerId].transform.SetParent(trackerParent);
        }
    }
}