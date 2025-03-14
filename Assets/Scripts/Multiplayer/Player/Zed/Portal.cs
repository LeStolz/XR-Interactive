using System.Collections.Generic;
using System.Linq;
using Multiplayer;
using Unity.Netcode;
using UnityEngine;

public class Portal : NetworkBehaviour
{
    readonly float cornerOffset = 0.17f;
    readonly float portalPlaneOffset = 0.01f;

    [SerializeField]
    GameObject[] markers;
    [SerializeField]
    GameObject ZEDCanvas;
    [field: SerializeField]
    public GameObject OutputPortal { get; private set; }
    [SerializeField]
    ZEDArUcoDetectionManager PortalCornersDetectionManager;

    Vector3[] portalCorners = new Vector3[4];
    readonly Calibrator calibrator = new(3, new float[] { 0.1f, 0.1f, 0.1f });

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
                    portalCorners[3] = portalCorners[0] - portalCorners[2] + portalCorners[1];

                    portalCorners = portalCorners.Select(v => v + normal * portalPlaneOffset).ToArray();

                    u = (portalCorners[2] - portalCorners[1]).normalized * cornerOffset;
                    v = (portalCorners[2] - portalCorners[0]).normalized * cornerOffset;

                    portalCorners[0] += u - v;
                    portalCorners[1] += -u + v;
                    portalCorners[2] += u + v;
                    portalCorners[3] += -u - v;

                    UpdatePortalRpc(portalCorners[0], portalCorners[1], portalCorners[2], portalCorners[3]);
                    ZEDCanvas.SetActive(false);
                }
            );
        }
    }

    [Rpc(SendTo.Everyone)]
    void UpdatePortalRpc(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        var v0 = vertex2;
        var v1 = vertex3;
        var v2 = vertex1;
        var v3 = vertex0;

        var w = (v1 - v0).magnitude / 10;
        var h = (v3 - v0).magnitude / 10;

        transform.position = v0 + (v1 - v0) / 2 + (v3 - v0) / 2;
        transform.up = Vector3.Cross(v1 - v0, v3 - v0).normalized;
        transform.localScale = new Vector3(w, 1, h);

        OutputPortal.transform.localScale = transform.localScale;
    }

    [Rpc(SendTo.Owner)]
    void RequestUpdatePortalRpc()
    {
        UpdatePortalRpc(portalCorners[0], portalCorners[1], portalCorners[2], portalCorners[3]);
    }
}
