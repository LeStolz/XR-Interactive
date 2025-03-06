using System.Collections.Generic;
using System.Linq;
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

    int iterations = 0;
    const int MAX_ITERATIONS = 30;
    readonly Vector3[] markerSums = new Vector3[3];
    readonly Vector3[] markerAverages = new Vector3[3];
    Vector3[] displayCorners = new Vector3[4];

    bool calibrating = true;

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
        calibrating = true;
        ZEDCanvas.SetActive(true);
    }

    void OnMarkersDetected(Dictionary<int, List<sl.Pose>> detectedposes)
    {
        if (!calibrating)
        {
            return;
        }

        if (IsOwner)
        {
            Vector3[] markerPosition = markers.Select(m => m.transform.position).ToArray();

            if (markers.All(m => m.activeSelf))
            {
                markerSums[0] += markerPosition[0];
                markerSums[1] += markerPosition[1];
                markerSums[2] += markerPosition[2];

                iterations++;

                if (iterations >= MAX_ITERATIONS)
                {


                    markerAverages[0] = markerSums[0] / MAX_ITERATIONS;
                    markerAverages[1] = markerSums[1] / MAX_ITERATIONS;
                    markerAverages[2] = markerSums[2] / MAX_ITERATIONS;

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

                    calibrating = false;
                    ZEDCanvas.SetActive(false);
                    iterations = 0;
                    markerSums[0] = Vector3.zero;
                    markerSums[1] = Vector3.zero;
                    markerSums[2] = Vector3.zero;
                }
            }
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
