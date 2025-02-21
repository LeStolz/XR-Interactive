using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Display : MonoBehaviour
{
    readonly float cornerOffset = 0.17f;
    readonly float displayPlaneOffset = 0.0f;

    [SerializeField]
    GameObject[] markers;

    [SerializeField]
    ZEDArUcoDetectionManager ZEDArUcoDetectionManager;

    int iterations = 0;
    const int MAX_ITERATIONS = 20;
    readonly Vector3[] markerSums = new Vector3[3];
    readonly Vector3[] markerAverages = new Vector3[3];

    void Start()
    {
        ZEDArUcoDetectionManager.OnMarkersDetected += OnMarkersDetected;
    }

    void OnMarkersDetected(Dictionary<int, List<sl.Pose>> detectedposes)
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

                Mesh mesh = new();
                Vector3[] vertices = new Vector3[4];

                vertices[0] = markerAverages[0];
                vertices[1] = markerAverages[1];
                vertices[2] = markerAverages[2];
                vertices[3] = vertices[0] - vertices[2] + vertices[1];

                vertices = vertices.Select(v => v + normal * displayPlaneOffset).ToArray();

                u = (vertices[2] - vertices[1]).normalized * cornerOffset;
                v = (vertices[2] - vertices[0]).normalized * cornerOffset;

                vertices[0] += u - v;
                vertices[1] += -u + v;
                vertices[2] += u + v;
                vertices[3] += -u - v;

                int[] triangles = new int[]
                {
                    0, 3, 1,
                    0, 1, 2
                };

                mesh.vertices = vertices;
                mesh.triangles = triangles;

                MeshFilter meshFilter = GetComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;


                iterations = 0;
                markerSums[0] = Vector3.zero;
                markerSums[1] = Vector3.zero;
                markerSums[2] = Vector3.zero;
            }
        }
    }
}
