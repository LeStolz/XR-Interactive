using System.Collections.Generic;
using System.Linq;
using Multiplayer;
using Unity.Netcode;
using UnityEngine;

public class Portal : NetworkBehaviour
{
    readonly float cornerOffset = 0.24f;
    readonly float portalPlaneOffset = 0.1f;

    [SerializeField]
    GameObject[] markers;
    [SerializeField]
    GameObject ZEDCanvas;
    [field: SerializeField]
    public GameObject OutputPortal { get; private set; }
    [SerializeField]
    ZEDArUcoDetectionManager PortalCornersDetectionManager;

    Vector3[] portalCorners = new Vector3[3];
    readonly Calibrator calibrator = new(3, new float[] { 0.5f, 0.5f, 0.5f });

    void Start()
    {
        PortalCornersDetectionManager.OnMarkersDetected += OnMarkersDetected;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            RequestUpdatePortalRpc();
        }
    }

    public void Calibrate()
    {
        calibrator.StartCalibration();
        ZEDCanvas.SetActive(true);
    }

    void OnMarkersDetected(Dictionary<int, List<sl.Pose>> detectedposes)
    {
        if (!IsOwner)
        {
            return;
        }

        Vector3[] markerPosition = markers.Select(m => m.transform.position).ToArray();

        if (markers.All(m => m.activeSelf))
        {
            calibrator.Calibrate(
                markerPosition,
                (markerAverages) =>
                {
                    Vector3 u = markerAverages[1] - markerAverages[0];
                    Vector3 v = markerAverages[2] - markerAverages[0];
                    Vector3 normal = Vector3.Cross(u, v).normalized;

                    portalCorners[0] = markerAverages[0];
                    portalCorners[1] = markerAverages[1];
                    portalCorners[2] = markerAverages[2];

                    portalCorners = portalCorners.Select(v => v + normal * portalPlaneOffset).ToArray();

                    u = (portalCorners[2] - portalCorners[1]).normalized * cornerOffset;
                    v = (portalCorners[2] - portalCorners[0]).normalized * cornerOffset;

                    portalCorners[0] += u - v;
                    portalCorners[1] += -u + v;
                    portalCorners[2] += u + v;

                    UpdatePortalRpc(portalCorners[0], portalCorners[1], portalCorners[2]);
                    ZEDCanvas.SetActive(false);
                }
            );
        }
    }

    [Rpc(SendTo.Everyone)]
    void UpdatePortalRpc(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
    {
        var v0 = vertex1;
        var v1 = vertex2;
        var v2 = vertex0;

        var w = (v1 - v0).magnitude / 10;
        var h = (v2 - v1).magnitude / 10;

        transform.SetPositionAndRotation(
            v0 + (v1 - v0) / 2 + (v2 - v1) / 2,
            Quaternion.LookRotation(v1 - v2, -Vector3.Cross(v0 - v1, v2 - v1))
        );
        transform.localScale = new Vector3(w, 1, h);

        OutputPortal.transform.localScale = transform.localScale;
    }

    [Rpc(SendTo.Owner)]
    void RequestUpdatePortalRpc()
    {
        UpdatePortalRpc(portalCorners[0], portalCorners[1], portalCorners[2]);
    }
}
