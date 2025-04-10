using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SpatialTracking;
using Main;

public class CalibrationManager : MonoBehaviour
{
    Vector3 HOLOLENS_GROUND_OFFSET = new(0, -0.013f, 0f);
    [SerializeField]
    float HOLOLENS_CAMERA_OFFSET = 0.01f;
    [SerializeField]
    float OCCULUS_CAMERA_OFFSET = -0.12f;

    readonly Calibrator calibrator = new(3, new float[] { 0.1f, 0.1f, 0.1f });
    public static CalibrationManager Instance = null;
    public GameObject XRPlaySpace;
    public GameObject XRCamera;
    public GameObject VirtualTrackingCamera;
    float trackMarkerDuration = 3f;
    public GameObject OpenCVMarkerTrackingModule;
    public GameObject OpenCVMarker;
    public GameObject MarkerSettings;
    float trackMarkerCountDown = 0f;
    public bool HMDMarkerTracking { get; set; } = false;
    public bool MarkerTracked { get; private set; }
    public GameObject CloneMarker { get; private set; }

    enum HeadSet
    {
        Hololens,
        Quest
    };

    [SerializeField]
    HeadSet headset;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (NetworkGameManager.Instance.localRole != Role.HMD)
        {
            Destroy(gameObject);
            Instance = null;
            return;
        }

        XRPlaySpace = NetworkGameManager.Instance.XRPlaySpace;
        XRCamera = NetworkGameManager.Instance.XRCamera;

        if (XRCamera.TryGetComponent(out TrackedPoseDriver trackedPoseDriver))
        {
            trackedPoseDriver.enabled = false;
        }

        XRCamera.transform.localPosition = Vector3.zero;
        XRCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        VirtualTrackingCamera.SetActive(true);
    }

    bool calibrating = false;
    void Update()
    {
        if (trackMarkerCountDown >= trackMarkerDuration && !MarkerTracked)
        {
            if (!calibrating)
            {
                calibrating = true;
                calibrator.StartCalibration();
            }

            calibrator.Calibrate(
            new Vector3[] {
                OpenCVMarker.transform.position,
                OpenCVMarker.transform.position + OpenCVMarker.transform.forward,
                OpenCVMarker.transform.position + OpenCVMarker.transform.up,
            },
            (averages) =>
            {
                var markerPositionAverage = averages[0];
                var markerForwardAverage = averages[1];
                var markerUpAverage = averages[2];

                OpenCVMarker.transform.SetPositionAndRotation(
                    markerPositionAverage,
                    Quaternion.LookRotation(
                        markerForwardAverage - markerPositionAverage,
                        markerUpAverage - markerPositionAverage
                    )
                );

                if (XRCamera.TryGetComponent(out TrackedPoseDriver trackedPoseDriver))
                {
                    trackedPoseDriver.enabled = true;
                }

                TurnOffMarkerTrackingModule();

                XRCamera.GetComponent<Camera>().fieldOfView = VirtualTrackingCamera.GetComponent<Camera>().fieldOfView;

                MarkerTracked = true;
            });
        }
        else
        {
            HMDMarkerTracking = OpenCVMarker.activeSelf;

            if (!MarkerTracked)
            {
                VirtualTrackingCamera.transform.SetParent(XRCamera.transform);
                VirtualTrackingCamera.transform.SetLocalPositionAndRotation(
                    headset == HeadSet.Hololens
                        ? new Vector3(0, HOLOLENS_CAMERA_OFFSET, 0)
                        : new Vector3(0, OCCULUS_CAMERA_OFFSET, 0),
                    Quaternion.identity
                );
                VirtualTrackingCamera.transform.position += HOLOLENS_GROUND_OFFSET;
                OpenCVMarker.transform.SetParent(VirtualTrackingCamera.transform);
            }
            if (HMDMarkerTracking)
            {
                trackMarkerCountDown += Time.deltaTime;
                Debug.Log(trackMarkerCountDown);
            }
            else
            {
                trackMarkerCountDown = 0;
            }
        }
    }

    void TurnOffMarkerTrackingModule()
    {
        CloneMarker = Instantiate(OpenCVMarker, parent: OpenCVMarker.transform);
        CloneMarker.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        CloneMarker.transform.localScale = Vector3.one;

        CloneMarker.transform.SetParent(VirtualTrackingCamera.transform);
        OpenCVMarker.transform.SetParent(OpenCVMarkerTrackingModule.transform);
        OpenCVMarkerTrackingModule.SetActive(false);

        StartCoroutine(IE_WaitForCondition(
            () =>
                XRCamera.transform.localPosition != Vector3.zero &&
                XRCamera.transform.localRotation != Quaternion.identity,
            () =>
            {
                PlayerHudNotification.Instance.ShowText("Calibrating");
                Calibrate();
            }
        ));
    }

    bool calibrated = false;
    void Calibrate()
    {
        if (calibrated) return;
        calibrated = true;

        CloneMarker.transform.SetParent(null);
        XRPlaySpace.transform.SetParent(CloneMarker.transform);
        CloneMarker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new(-90f, 180f, 0f)));
        XRPlaySpace.transform.SetParent(null);
        XRPlaySpace.transform.eulerAngles = new(
                                               SnapToMutiplyOf(XRPlaySpace.transform.eulerAngles.x, 360f),
                                               XRPlaySpace.transform.eulerAngles.y,
                                               SnapToMutiplyOf(XRPlaySpace.transform.eulerAngles.z, 360f)
                                           );
        VirtualTrackingCamera.SetActive(false);
    }

    public float SnapToMutiplyOf(float value, float baseVal)
    {
        return (float)Math.Round(value / baseVal) * baseVal;
    }

    IEnumerator IE_WaitForCondition(Func<bool> condition, Action action)
    {
        yield return new WaitUntil(condition);
        action.Invoke();
    }

    public void Recalibrate()
    {
        MarkerTracked = false;
        calibrated = false;
        calibrating = false;
        trackMarkerCountDown = 0f;

        OpenCVMarkerTrackingModule.SetActive(true);
        OpenCVMarker.transform.SetParent(MarkerSettings.transform);
        OpenCVMarker.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        Destroy(CloneMarker);
        VirtualTrackingCamera.transform.SetParent(OpenCVMarkerTrackingModule.transform);

        XRPlaySpace.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (XRCamera.TryGetComponent(out TrackedPoseDriver trackedPoseDriver))
        {
            trackedPoseDriver.enabled = false;
        }

        XRCamera.transform.localPosition = Vector3.zero;
        VirtualTrackingCamera.SetActive(true);
    }

}
