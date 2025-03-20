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

            if (IsOwner)
            {
                transform.SetPositionAndRotation(new Vector3(
                    PlayerPrefs.GetFloat("ServerTrackerManagerPositionX", 0),
                    PlayerPrefs.GetFloat("ServerTrackerManagerPositionY", 0),
                    PlayerPrefs.GetFloat("ServerTrackerManagerPositionZ", 0)),
                    Quaternion.Euler(new Vector3(
                        PlayerPrefs.GetFloat("ServerTrackerManagerRotationX", 0),
                        PlayerPrefs.GetFloat("ServerTrackerManagerRotationY", 0),
                        PlayerPrefs.GetFloat("ServerTrackerManagerRotationZ", 0)
                    ))
                );
            }
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

            PlayerPrefs.SetFloat("ServerTrackerManagerPositionX", transform.position.x);
            PlayerPrefs.SetFloat("ServerTrackerManagerPositionY", transform.position.y);
            PlayerPrefs.SetFloat("ServerTrackerManagerPositionZ", transform.position.z);
            PlayerPrefs.SetFloat("ServerTrackerManagerRotationX", transform.rotation.eulerAngles.x);
            PlayerPrefs.SetFloat("ServerTrackerManagerRotationY", transform.rotation.eulerAngles.y);
            PlayerPrefs.SetFloat("ServerTrackerManagerRotationZ", transform.rotation.eulerAngles.z);

            CalibrateRpc(transform.position, transform.rotation.eulerAngles);
        }

        [Rpc(SendTo.NotMe)]
        void CalibrateRpc(Vector3 position, Vector3 rotation)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }
    }
}