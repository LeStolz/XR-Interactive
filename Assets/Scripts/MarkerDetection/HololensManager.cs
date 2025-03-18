using Microsoft.MixedReality.Toolkit.Input;
using MyTools;
using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SpatialTracking;

public partial class GameManager
{
    private float trackMarkerCountDown = 0f;
    public bool HololensMarkerTracking { get => hololensMarkerTracking; set => hololensMarkerTracking = value; }
    private bool hololensMarkerTracking = false;

    private GameObject playerModel;
    public GameObject PlayerModel { get => playerModel; }

    public bool TrackedWithVuforia { get => trackedWithVuforia; private set => trackedWithVuforia = value; }
    private bool trackedWithVuforia = false;

    public bool MarkerTracked { get => _markerTracked; private set => _markerTracked = value; }

    private bool _markerTracked = false;

    public void InitHololens()
    {
        ARPlaySpace = ARCamera.transform.parent;
        ARCamera.transform.localPosition = Vector3.zero;
        playerModel = PhotonNetwork.Instantiate("Prefabs/Player/" + playerPrefab.name, ARCamera.transform.position, ARCamera.transform.rotation);
        ARCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        VirtualTrackingCamera.SetActive(true);

        if (playerModel.GetComponent<PhotonView>().IsMine)
        {
            playerModel.GetComponent<MoveARCamera>().ARCamera = ARCamera.transform;
        }


        init = true;
    }

    public void UpdateHololens()
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
    private GameObject cloneMarker;
    public GameObject CloneMarker { get => cloneMarker; private set => cloneMarker = value; }

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


        Debug.Log("Broadcast");
        this.Broadcast(EventID.FINISH_MARKER_TRACKING);
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
