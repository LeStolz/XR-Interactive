using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    class ZEDModelManager : NetworkPlayer
    {
        public readonly float HEIGHT_OFFSET_FROM_TRACKER = -1.52f;

        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        GameObject ZEDModel;
        [SerializeField]
        GameObject calibrationPoint;
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
        readonly Calibrator calibrator = new(2, new float[] { 0.5f, 720f });

        [SerializeField]
        Tracker[] trackers;

        ServerTrackerManager serTrackerManager;

        void Start()
        {
            originDetectionManager.OnMarkersDetected += OnMarkersDetected;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                NetworkRoleManager.Instance.MRInteractionSetup.SetActive(false);

                foreach (var obj in objectsToEnableOnSpawn)
                {
                    obj.SetActive(true);
                }

                NetworkRoleManager.Instance.TableUI.SetActive(false);

                calibrator.StartCalibration();
            }
        }

        [Rpc(SendTo.Owner)]
        public void RequestCalibrationRpc()
        {
            serTrackerManager = FindFirstObjectByType<ServerTrackerManager>();
            if (serTrackerManager != null)
            {
                serTrackerManager.CalibrateRpc(
                    calibrationPoint.transform.position + new Vector3(0, HEIGHT_OFFSET_FROM_TRACKER, 0),
                    calibrationPoint.transform.forward
                );
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                NetworkRoleManager.Instance.MRInteractionSetup.SetActive(true);
                NetworkRoleManager.Instance.TableUI.SetActive(true);
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

                if (serTrackerManager != null)
                {
                    for (int i = 0; i < trackers.Length; i++)
                    {
                        trackers[i].transform.SetPositionAndRotation(
                            serTrackerManager.Trackers[i].transform.position,
                            serTrackerManager.Trackers[i].transform.rotation
                        );

                        trackers[i].StartRayCastAndTeleport(
                            leftEye.GetComponent<Camera>(),
                            (hitMarkerId, start, end) => DrawLineRpc(i, hitMarkerId, start, end)
                        );
                    }
                }
            }
        }

        void OnMarkersDetected(Dictionary<int, List<sl.Pose>> detectedposes)
        {
            if (IsOwner)
            {
                calibrator.Calibrate(
                    new Vector3[] { marker.transform.position, marker.transform.rotation.eulerAngles },
                    (averages) =>
                    {
                        var markerPositionAverage = averages[0];
                        var markerRotationAverage = averages[1];

                        marker.transform.SetPositionAndRotation(
                            markerPositionAverage,
                            Quaternion.Euler(markerRotationAverage)
                        );

                        var parent = cameraEyes.transform.parent;

                        cameraEyes.transform.SetPositionAndRotation(leftEye.transform.position, leftEye.transform.rotation);

                        cameraEyes.transform.SetParent(marker.transform);
                        marker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new(-90, 0, 0)));
                        cameraEyes.transform.SetParent(parent);

                        leftEye.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);

                        inputPortal.Calibrate();

                        ZEDModel.transform.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);

                        serTrackerManager = FindFirstObjectByType<ServerTrackerManager>();
                        if (serTrackerManager != null)
                        {
                            serTrackerManager.CalibrateRpc(
                                calibrationPoint.transform.position + new Vector3(0, HEIGHT_OFFSET_FROM_TRACKER, 0),
                                calibrationPoint.transform.forward
                            );
                        }
                    }
                );
            }
        }

        [Rpc(SendTo.Everyone)]
        public void DrawLineRpc(int trackerId, int hitMarkerId, Vector3 start, Vector3 end)
        {
            trackers[trackerId].DrawLine(hitMarkerId, start, end);
        }
    }
}