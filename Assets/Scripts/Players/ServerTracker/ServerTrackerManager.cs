using Unity.Netcode;
using UnityEngine;

namespace Main
{
    class ServerTrackerManager : NetworkPlayer
    {
        [SerializeField]
        Vector3 OFFSET;
        [field: SerializeField]
        public Tracker[] Trackers { get; private set; }
        [SerializeField]
        new GameObject camera;
        Calibrator calibrator = new(3, new[] { 0.1f, 0.1f, 0.1f });
        int calibratingTrackerId = -1;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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

                CalibrateRpc(transform.position, transform.rotation.eulerAngles);
            }
        }

        void Update()
        {
            if (calibratingTrackerId < 0 || calibratingTrackerId >= Trackers.Length)
            {
                return;
            }

            calibrator.Calibrate(
                new[] {
                    Trackers[calibratingTrackerId].transform.position,
                    Trackers[calibratingTrackerId].transform.forward,
                    Trackers[calibratingTrackerId].transform.up
                },
                (averages) =>
                {
                    var trackerPositionAverage = averages[0];
                    var trackerForwardAverage = averages[1];
                    var trackerUpAverage = averages[2];

                    Trackers[calibratingTrackerId].transform.SetPositionAndRotation(
                        trackerPositionAverage,
                        Quaternion.LookRotation(
                            trackerForwardAverage - trackerPositionAverage,
                            trackerUpAverage - trackerPositionAverage
                        )
                    );

                    var trackerId = calibratingTrackerId;
                    var trackerParent = Trackers[trackerId].transform.parent;
                    var thisParent = transform.parent;

                    Trackers[trackerId].transform.position = averages[0];
                    Trackers[trackerId].transform.rotation.SetLookRotation(averages[1], averages[2]);

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

                    calibratingTrackerId = -1;
                }
            );
        }

        public void Calibrate(int trackerId)
        {
            if (trackerId < 0 || trackerId >= Trackers.Length)
            {
                return;
            }

            calibrator.StartCalibration();
        }

        [Rpc(SendTo.NotMe)]
        void CalibrateRpc(Vector3 position, Vector3 rotation)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }
    }
}