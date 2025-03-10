using System.Collections.Generic;
using Mono.Cecil.Cil;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    class ZedModelManager : NetworkPlayer
    {
        const float HEIGHT_OFFSET_FROM_TRACKER = -1.45f;

        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        GameObject zedModel;
        [SerializeField]
        GameObject cameraEyes;
        [SerializeField]
        Display display;
        [SerializeField]
        GameObject marker;
        [SerializeField]
        Transform leftEye;
        [SerializeField]
        ZEDArUcoDetectionManager originDetectionManager;

        int iterations = 0;
        const int MAX_ITERATIONS = 30;
        Vector3 markerPositionSum = Vector3.zero;
        Vector3 markerRotationSum = Vector3.zero;
        bool calibrating = true;

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
            }
        }

        [Rpc(SendTo.Owner)]
        public void RequestCalibrationRpc()
        {
            var serTrackerManager = FindFirstObjectByType<SerTrackerManager>();
            if (serTrackerManager != null)
            {
                serTrackerManager.CalibrateRpc(
                    zedModel.transform.position + new Vector3(0, HEIGHT_OFFSET_FROM_TRACKER, 0),
                    zedModel.transform.rotation.eulerAngles
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
                    calibrating = true;
                }
            }
        }

        void OnMarkersDetected(Dictionary<int, List<sl.Pose>> detectedposes)
        {
            if (IsOwner)
            {
                if (calibrating)
                {
                    markerPositionSum += marker.transform.position;
                    markerRotationSum += marker.transform.rotation.eulerAngles;

                    iterations++;

                    if (iterations >= MAX_ITERATIONS)
                    {
                        var markerPositionAverage = markerPositionSum / MAX_ITERATIONS;
                        var markerRotationAverage = markerRotationSum / MAX_ITERATIONS;

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

                        calibrating = false;
                        iterations = 0;
                        markerPositionSum = Vector3.zero;
                        markerRotationSum = Vector3.zero;

                        display.Calibrate();

                        var serTrackerManager = FindFirstObjectByType<SerTrackerManager>();
                        if (serTrackerManager != null)
                        {
                            serTrackerManager.CalibrateRpc(
                                zedModel.transform.position + new Vector3(0, HEIGHT_OFFSET_FROM_TRACKER, 0),
                                zedModel.transform.rotation.eulerAngles
                            );
                        }
                    }
                }

                zedModel.transform.SetPositionAndRotation(cameraEyes.transform.position, cameraEyes.transform.rotation);
            }
        }
    }
}