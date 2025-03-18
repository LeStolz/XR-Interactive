using System.Collections;
using UnityEngine;

public class MarkerTracking : MonoBehaviour
{
    static float MARKER_LENGTH_IN_METERS_REAL = .433f;
    static float MARKER_ERROR = 1.45f / 1.9f;

    public static float MARKER_LENGTH_IN_METERS => MARKER_LENGTH_IN_METERS_REAL * MARKER_ERROR;

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
            ARMarker.transform.localScale = new Vector3(-1, 1, 1) * MARKER_LENGTH_IN_METERS;
        }
    }
}
