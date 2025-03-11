using System.Collections.Generic;
using System.Linq;
using Multiplayer;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Display : NetworkBehaviour
{
    readonly float cornerOffset = 0.17f;
    readonly float displayPlaneOffset = 0.01f;

    [SerializeField]
    GameObject[] markers;
    [SerializeField]
    GameObject ZEDCanvas;
    [SerializeField]
    ZEDArUcoDetectionManager DisplayCornersDetectionManager;

    Vector3[] displayCorners = new Vector3[4];
    readonly Calibrator calibrator = new(3);

    void Start()
    {
        DisplayCornersDetectionManager.OnMarkersDetected += OnMarkersDetected;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            RequestUpdateDisplayRpc();
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

                    displayCorners[0] = markerAverages[0];
                    displayCorners[1] = markerAverages[1];
                    displayCorners[2] = markerAverages[2];
                    displayCorners[3] = displayCorners[0] - displayCorners[2] + displayCorners[1];

                    displayCorners = displayCorners.Select(v => v + normal * displayPlaneOffset).ToArray();

                    u = (displayCorners[2] - displayCorners[1]).normalized * cornerOffset;
                    v = (displayCorners[2] - displayCorners[0]).normalized * cornerOffset;

                    displayCorners[0] += u - v;
                    displayCorners[1] += -u + v;
                    displayCorners[2] += u + v;
                    displayCorners[3] += -u - v;

                    UpdateDisplayRpc(displayCorners[0], displayCorners[1], displayCorners[2], displayCorners[3]);
                    ZEDCanvas.SetActive(false);
                }
            );
        }
    }

    [Rpc(SendTo.Everyone)]
    void UpdateDisplayRpc(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        int[] triangles = new int[]
        {
            0, 3, 1,
            0, 1, 2
        };

        Vector3[] vertices = new Vector3[]
        {
            vertex0,
            vertex1,
            vertex2,
            vertex3
        };

        Mesh displayMesh = new()
        {
            vertices = vertices,
            triangles = triangles
        };

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = displayMesh;
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = displayMesh;
    }

    [Rpc(SendTo.Owner)]
    void RequestUpdateDisplayRpc()
    {
        UpdateDisplayRpc(displayCorners[0], displayCorners[1], displayCorners[2], displayCorners[3]);
    }
}
