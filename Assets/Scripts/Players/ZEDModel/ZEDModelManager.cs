using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace Main
{
    class ZEDModelManager : NetworkPlayer
    {
        const float HEIGHT_OFFSET = 0.052f;

        [SerializeField]
        GameObject[] objectsToEnableOnSpawn;
        [SerializeField]
        GameObject camerasContainer;
        [SerializeField]
        Portal inputPortal;
        [SerializeField]
        GameObject marker;
        [SerializeField]
        Transform leftEye;
        [SerializeField]
        GameObject frame;
        [SerializeField]
        ZEDArUcoDetectionManager originDetectionManager;
        readonly Calibrator calibrator = new(3, new float[] { 0.2f, 0.2f, 0.2f });

        ServerTrackerManager serverTrackerManager;
        [field: SerializeField]
        public HitMarker[] HitMarkers { get; private set; }

        [SerializeField]
        TMP_Text debugText;

        void Start()
        {
            originDetectionManager.OnMarkersDetected += OnMarkersDetected;
            originDetectionManager.markerWidthMeters = GlobalMarkerConfigs.VIRTUAL_ORIGIN_MAKRER;
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

                if (!frame.activeSelf)
                {
                    StartCoroutine(ActivateFrame());
                }
            }
        }

        IEnumerator ActivateFrame()
        {
            yield return new WaitForSeconds(0.5f);

            if (!frame.activeSelf)
                frame.SetActive(true);
        }

        [Rpc(SendTo.Owner)]
        public void UpdateRaySpaceRpc(int id, int raySpaceId)
        {
            debugText.text = $"{(Tracker.RaySpace)raySpaceId}";
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

                        var parent = camerasContainer.transform.parent;

                        camerasContainer.transform.SetPositionAndRotation(leftEye.transform.position, leftEye.transform.rotation);

                        camerasContainer.transform.SetParent(marker.transform);
                        marker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new(-90, 0, 0)));
                        camerasContainer.transform.SetParent(parent);

                        camerasContainer.transform.position = new(
                            camerasContainer.transform.position.x,
                            camerasContainer.transform.position.y + HEIGHT_OFFSET,
                            camerasContainer.transform.position.z
                        );
                        leftEye.SetPositionAndRotation(camerasContainer.transform.position, camerasContainer.transform.rotation);

                        inputPortal.Calibrate();
                    }
                );
            }
        }
    }
}
