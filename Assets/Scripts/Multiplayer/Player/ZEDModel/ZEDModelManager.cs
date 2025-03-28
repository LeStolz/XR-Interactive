using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    class ZEDModelManager : NetworkPlayer
    {
        [SerializeField]
        float ORIGIN_MARKER_HEIGHT_OFFSET = 0.06f;
        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        GameObject ZEDModel;
        [SerializeField]
        GameObject cameraEyes;
        [SerializeField]
        Portal inputPortal;
        [SerializeField]
        GameObject marker;
        [SerializeField]
        Transform leftEye;
        [SerializeField]
        ZEDArUcoDetectionManager originDetectionManager;
        readonly Calibrator calibrator = new(3, new float[] { 0.2f, 0.2f, 0.2f });

        ServerTrackerManager serverTrackerManager;
        [field: SerializeField]
        public HitMarker[] HitMarkers { get; private set; }

        void Start()
        {
            originDetectionManager.OnMarkersDetected += OnMarkersDetected;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                NetworkGameManager.Instance.MRInteractionSetup.SetActive(false);

                foreach (var obj in objectsToEnableOnSpawn)
                {
                    obj.SetActive(true);
                }

                NetworkGameManager.Instance.TableUI.SetActive(false);

                calibrator.StartCalibration();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                NetworkGameManager.Instance.MRInteractionSetup.SetActive(true);
                NetworkGameManager.Instance.TableUI.SetActive(true);
            }
        }

        void Update()
        {
            if (IsOwner)
            {
                if (Input.GetKeyDown(KeyCode.E) && marker.activeSelf)
                {
                    calibrator.StartCalibration();
                }

                if (serverTrackerManager != null)
                {
                    for (int i = 0; i < serverTrackerManager.Trackers.Length; i++)
                    {
                        serverTrackerManager.Trackers[i].StartRayCastAndTeleport(
                            leftEye.GetComponent<Camera>()
                        );
                    }
                }
                else
                {
                    serverTrackerManager = NetworkGameManager.Instance.FindPlayerByRole<ServerTrackerManager>(Role.ServerTracker);
                }
            }
        }

        void OnMarkersDetected(Dictionary<int, List<sl.Pose>> detectedposes)
        {
            if (IsOwner)
            {
                calibrator.Calibrate(
                    new Vector3[] {
                        marker.transform.position,
                        marker.transform.position + marker.transform.forward,
                        marker.transform.position + marker.transform.up,
                    },
                    (averages) =>
                    {
                        var markerPositionAverage = averages[0];
                        var markerForwardAverage = averages[1];
                        var markerUpAverage = averages[2];

                        marker.transform.SetPositionAndRotation(
                            markerPositionAverage,
                            Quaternion.LookRotation(
                                markerForwardAverage - markerPositionAverage,
                                markerUpAverage - markerPositionAverage
                            )
                        );

                        var parent = cameraEyes.transform.parent;

                        cameraEyes.transform.SetPositionAndRotation(leftEye.transform.position, leftEye.transform.rotation);

                        cameraEyes.transform.SetParent(marker.transform);
                        marker.transform.SetPositionAndRotation(
                            new(0, ORIGIN_MARKER_HEIGHT_OFFSET, 0), Quaternion.Euler(new(-90, 0, 0))
                        );
                        cameraEyes.transform.SetParent(parent);

                        leftEye.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);

                        inputPortal.Calibrate();

                        ZEDModel.transform.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);
                    }
                );
            }
        }
    }
}