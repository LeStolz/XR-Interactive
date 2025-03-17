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

        SerTrackerManager serTrackerManager;
        TrackerManager TrackerManager
        {
            get => serTrackerManager.TrackerManager;
        }

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
            serTrackerManager = FindFirstObjectByType<SerTrackerManager>();
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

                if (TrackerManager != null)
                    RayCastAndTeleport(
                        new Ray(
                            TrackerManager.Arrow.transform.position,
                            TrackerManager.Arrow.transform.forward
                        ), TrackerManager.HitMarkers.Length - 1
                    );
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

                        serTrackerManager = FindFirstObjectByType<SerTrackerManager>();
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

        void RayCastAndTeleport(Ray ray, int depth)
        {
            if (depth < 0)
            {
                return;
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Room")))
            {
                TrackerManager.HitMarkers[depth].transform.position = hit.point;
                TrackerManager.HitMarkers[depth].transform.forward = hit.normal;

                serTrackerManager.DrawLineRpc(
                    depth,
                    ray.origin, TrackerManager.HitMarkers[depth].transform.position
                );

                if (!hit.transform.gameObject.CompareTag("InputPortal"))
                {
                    return;
                }

                var inputPortal = hit.transform.gameObject.GetComponent<Portal>();
                var outputPortal = leftEye.GetComponent<Camera>();

                RayCastAndTeleport(
                    outputPortal.ScreenPointToRay(inputPortal.PortalSpaceToScreenSpace(hit.point, outputPortal)),
                    depth - 1
                );
            }
            else
            {
                serTrackerManager.DrawLineRpc(
                    depth,
                    ray.origin, ray.origin + ray.direction * 10
                );

                while (depth >= 0)
                {
                    TrackerManager.HitMarkers[depth].transform.position = new(0, -10, 0);
                    depth--;
                }
            }
        }

    }
}