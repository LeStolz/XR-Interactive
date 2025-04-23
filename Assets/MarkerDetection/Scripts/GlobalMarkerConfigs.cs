using System.Collections;
using UnityEngine;

public class GlobalMarkerConfigs : MonoBehaviour
{
    static readonly float REAL_ORIGIN_MARKER = 0.433f;
    static readonly float REAL_PORTAL_MARKER = 0.336f;

    public static float VIRTUAL_HOLOLENS_MARKER => REAL_ORIGIN_MARKER * 0.8f;
    public static float VIRTUAL_ORIGIN_MAKRER => REAL_ORIGIN_MARKER * 0.961f;
    public static float VIRTUAL_PORTAL_MARKER => REAL_PORTAL_MARKER * 0.961f;

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
