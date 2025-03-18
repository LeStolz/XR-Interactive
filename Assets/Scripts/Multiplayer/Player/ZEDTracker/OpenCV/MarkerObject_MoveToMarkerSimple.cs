
using UnityEngine;

/// <summary>
/// Moves the object to the marker's location each grab, and turns itself off when it's not seen if desired.
/// This is a stripped down vesion of MarkerObject_MoveToMarker meant to be as simple an implementation as
/// possible for use as a reference. The regular MarkerObject_MoveToMarker includes smoothing and hide-delay
/// features that make it more useful.
/// </summary>
public class MarkerObject_MoveToMarkerSimple : MarkerObject
{
    public bool HideWhenNotSeen = true;

    public override void MarkerDetectedSingle(Vector3 worldposition, Quaternion worldrotation)
    {
        if (HideWhenNotSeen)
            gameObject.SetActive(true);
        transform.SetPositionAndRotation(worldposition, worldrotation);
    }

    public override void MarkerNotDetected()
    {
        if (HideWhenNotSeen)
            gameObject.SetActive(false);
    }
}
