using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public static class Constants
{
    public static float MARKER_LENGTH_IN_METERS_REAL = .433f;
    public static float MARKER_ERROR = (1.45f / 1.9f);
    public static int DEFAULT_RING_TOTAL = 3;
    public static int MAX_RING = 10;
    public static int DEFAULT_SPHERE_PER_RING = 6;
    public static int MAX_SPHERE = 10;
    public static int MAX_ACTIVATION_PER_SPHERE = 2;

    public static float MARKER_LENGTH_IN_METERS => MARKER_LENGTH_IN_METERS_REAL * MARKER_ERROR;
    public static string POV_COMBINED = "Combine";
    public static string POV_THIRD_PERSON = "TPV";
}
