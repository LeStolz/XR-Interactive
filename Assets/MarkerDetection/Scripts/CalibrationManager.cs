using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SpatialTracking;
using Multiplayer;

public class CalibrationManager : MonoBehaviour
{
    readonly Calibrator calibrator = new(2, new float[] { 0.5f, 720f });
    public static CalibrationManager Instance = null;
    public Transform XRPlaySpace;
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

    enum HeadSet {
        Hololens,
        Quest
    };

    [SerializeField]
    HeadSet headset;

    [SerializeField]
    GameObject canvas;

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
        } else {
            canvas.SetActive(true);
        }

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
            new Vector3[] { OpenCVMarker.transform.position, OpenCVMarker.transform.rotation.eulerAngles },
            (averages) =>
            {
                var markerPositionAverage = averages[0];
                var markerRotationAverage = averages[1];

                OpenCVMarker.transform.SetPositionAndRotation(
                    markerPositionAverage,
                    Quaternion.Euler(markerRotationAverage)
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
                        ? new Vector3(0, -0.06f, 0)
                        : new Vector3(0, -0.15f, 0),
                    Quaternion.identity
                );
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
        VirtualTrackingCamera.SetActive(false);
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
