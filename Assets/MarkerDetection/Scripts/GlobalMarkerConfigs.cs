using System.Collections;
using UnityEngine;

public class GlobalMarkerConfigs : MonoBehaviour
{
    static readonly float VIRTUAL_MARKER_TO_REAL_MARKER_RATIO = 0.8f;

    static readonly float REAL_ORIGIN_MARKER = 0.433f;
    static readonly float REAL_PORTAL_MARKER = 0.34f;

    public static float VIRTUAL_HOLOLENS_MARKER => REAL_ORIGIN_MARKER * VIRTUAL_MARKER_TO_REAL_MARKER_RATIO;
    public static float VIRTUAL_ORIGIN_MAKRER => REAL_ORIGIN_MARKER / VIRTUAL_MARKER_TO_REAL_MARKER_RATIO;
    public static float VIRTUAL_PORTAL_MARKER => REAL_PORTAL_MARKER / VIRTUAL_MARKER_TO_REAL_MARKER_RATIO;

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
            ARMarker.transform.localScale = new Vector3(-1, 1, 1) * REAL_ORIGIN_MARKER;
        }
    }
}
