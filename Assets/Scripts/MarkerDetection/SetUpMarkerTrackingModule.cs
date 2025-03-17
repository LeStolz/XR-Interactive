using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetUpMarkerTrackingModule : MonoBehaviour
{
    public GameObject WebcamTextureMarkerTracking;
    public GameObject ARMarker;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        WebcamTextureMarkerTracking.SetActive(true);
    }


    private void Update()
    {
        if (ARMarker != null)
        {
            ARMarker.transform.localScale = new Vector3(-1 , 1, 1) * Constants.MARKER_LENGTH_IN_METERS;
        }
    }
}
