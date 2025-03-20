using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SpatialTracking;
using Multiplayer;

public class CalibrationManager : MonoBehaviour
{
    public static CalibrationManager Instance = null;

    public Transform XRPlaySpace;
    public GameObject XRCamera;
    public GameObject VirtualTrackingCamera;
    public float trackMarkerDuration = 3000f;
    public GameObject OpenCVMarkerTrackingModule;
    public GameObject OpenCVMarker;

    float trackMarkerCountDown = 0f;
    public bool HMDMarkerTracking { get; set; } = false;
    public bool MarkerTracked { get; private set; }
    public GameObject CloneMarker { get; private set; }

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

        if (XRCamera.TryGetComponent(out TrackedPoseDriver trackedPoseDriver))
        {
            trackedPoseDriver.enabled = false;
        }

        XRCamera.transform.localPosition = Vector3.zero;
        XRCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        VirtualTrackingCamera.SetActive(true);
    }

    void Update()
    {
        if (trackMarkerCountDown >= trackMarkerDuration && !MarkerTracked)
        {
            if (XRCamera.TryGetComponent(out TrackedPoseDriver trackedPoseDriver))
            {
                trackedPoseDriver.enabled = true;
            }

            TurnOffMarkerTrackingModule();

            XRCamera.GetComponent<Camera>().fieldOfView = VirtualTrackingCamera.GetComponent<Camera>().fieldOfView;
            MarkerTracked = true;
        }
        else
        {
            HMDMarkerTracking = OpenCVMarker.activeSelf;

            if (!MarkerTracked)
            {
                VirtualTrackingCamera.transform.SetParent(XRCamera.transform);
                VirtualTrackingCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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
        PlayerHudNotification.Instance.ShowText("Marker tracked");

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
        Destroy(VirtualTrackingCamera);
    }

    IEnumerator IE_WaitForCondition(Func<bool> condition, Action action)
    {
        yield return new WaitUntil(condition);
        action.Invoke();
    }
}