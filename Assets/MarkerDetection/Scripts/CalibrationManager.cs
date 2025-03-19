using UnityEngine;

using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections;
using UnityEngine.SpatialTracking;
using Multiplayer;

public class CalibrationManager : MonoBehaviour
{
    public static CalibrationManager Instance = null;

    [Header("Hololens")]
    public Transform ARPlaySpace;
    public GameObject ARCamera;
    public GameObject VirtualTrackingCamera;
    public float trackMarkerDuration = 3000f;
    public GameObject OpenCVMarkerTrackingModule;
    public GameObject OpenCVMarker;

    float trackMarkerCountDown = 0f;
    public bool HololensMarkerTracking { get => hololensMarkerTracking; set => hololensMarkerTracking = value; }
    bool hololensMarkerTracking = false;

    readonly GameObject playerModel;

    public bool MarkerTracked { get; private set; }

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
        if (NetworkGameManager.Instance.localRole != Role.HMD || NetworkGameManager.Instance.localRole != Role.ServerTracker)
        {
            Destroy(gameObject);
            Instance = null;
            return;
        }

        EnablePoseTrackingComponents(false);

        ARPlaySpace = ARCamera.transform.parent;
        ARCamera.transform.localPosition = Vector3.zero;
        playerModel.transform.SetPositionAndRotation(ARCamera.transform.position, ARCamera.transform.rotation);
        ARCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        VirtualTrackingCamera.SetActive(true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
        {
            ARCamera.transform.SetLocalPositionAndRotation(new(0f, 10f, 0f), Quaternion.Euler(new(0f, 10f, 0f)));
        }
#endif
        if (trackMarkerCountDown >= trackMarkerDuration && !MarkerTracked)
        {
            EnablePoseTrackingComponents(true);
            TurnOffMarkerTrackingModule();
            ARCamera.GetComponent<Camera>().fieldOfView = VirtualTrackingCamera.GetComponent<Camera>().fieldOfView;
            MarkerTracked = true;
        }
        else
        {
            HololensMarkerTracking = OpenCVMarker.activeSelf;
            if (!MarkerTracked)
            {
                UpdateARSpace();
            }
            if (HololensMarkerTracking)
            {
                trackMarkerCountDown += Time.deltaTime;
            }
            else
            {
                trackMarkerCountDown = 0;
            }
            Debug.Log(trackMarkerCountDown);
        }
    }

    private void UpdateARSpace()
    {
        VirtualTrackingCamera.transform.SetParent(ARCamera.transform);
        VirtualTrackingCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        OpenCVMarker.transform.SetParent(VirtualTrackingCamera.transform);
    }

    public GameObject CloneMarker { get; private set; }

    private void TurnOffMarkerTrackingModule()
    {
        Debug.Log("Turn off Marker Tracking Module");
        CloneMarker = Instantiate(OpenCVMarker.gameObject, parent: OpenCVMarker.transform);
        CloneMarker.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        CloneMarker.transform.localScale = Vector3.one;

        CloneMarker.transform.SetParent(VirtualTrackingCamera.transform);
        OpenCVMarker.transform.SetParent(OpenCVMarkerTrackingModule.transform);
        OpenCVMarkerTrackingModule.SetActive(false);


        if ((ARCamera.transform.localPosition != Vector3.zero) && (ARCamera.transform.localRotation != Quaternion.identity))
        {
            SetUp(CloneMarker);
        }
        else
        {
            StartCoroutine(IE_WaitForCondition(() => (ARCamera.transform.localPosition != Vector3.zero) && (ARCamera.transform.localRotation != Quaternion.identity),
                () =>
                {
                    SetUp(CloneMarker);
                }));
        }
    }

    private bool _setUp = false;
    private void SetUp(GameObject cloneMarker)
    {
        if (_setUp) return;
        _setUp = true;

        Destroy(VirtualTrackingCamera);
        cloneMarker.transform.SetParent(null);
        ARPlaySpace.transform.SetParent(cloneMarker.transform);
        cloneMarker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new(-90f, 0f, 0f)));
        ARPlaySpace.transform.SetParent(null);
        ARPlaySpace.transform.eulerAngles = new(
                                                snapToMutiplyOf(ARPlaySpace.transform.eulerAngles.x, 360f),
                                                ARPlaySpace.transform.eulerAngles.y,
                                                snapToMutiplyOf(ARPlaySpace.transform.eulerAngles.z, 360f)
                                            );

    }

    public float snapToMutiplyOf(float value, float baseVal)
    {
        return (float)Math.Round(value / baseVal) * baseVal;
    }

    public IEnumerator IE_WaitForCondition(Func<bool> condition, Action action)
    {
        yield return new WaitUntil(condition);
        action.Invoke();
    }

    public void ForceStopTrackingOnHololens()
    {
        trackMarkerDuration = 1f;
        trackMarkerCountDown = trackMarkerDuration + 1f;
    }


    private void EnablePoseTrackingComponents(bool enable)
    {
        if (ARCamera.TryGetComponent(out MixedRealityInputModule mrtkInputModule))
        {
            //mrtkInputModule.enabled = enable;
            if (enable)
            {
                mrtkInputModule.enabled = enable;
            }
            else
            {
                Destroy(mrtkInputModule);
            }
        }
        else if (enable)
        {
            ARCamera.AddComponent<MixedRealityInputModule>();
        }

        if (ARCamera.TryGetComponent(out TrackedPoseDriver trackedPoseDriver))
        {
            //trackedPoseDriver.enabled = enable;
            if (enable)
            {
                trackedPoseDriver.enabled = enable;
            }
            else
            {
                Destroy(trackedPoseDriver);
            }
        }
        else if (enable)
        {
            var obj = ARCamera.AddComponent<TrackedPoseDriver>();
            //  obj.UseRelativeTransform = true;
        }
        //if (ARCamera.TryGetComponent(out GazeProvider gazeProvider))
        //{
        //    if (enable)
        //    {
        //        gazeProvider.enabled = enable;
        //    }
        //    else
        //    {
        //        Destroy(gazeProvider);
        //    }
        //}
        //else if (enable)
        //{
        //    ARCamera.AddComponent<GazeProvider>();
        //}
    }
}