using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public GameObject socketPrefab;
    public GameObject dicePrefab;
    public int rows = 4;
    public int columns = 4;
    public static float socketGap = 0.01f;

    private void Start()
    {
        SpawnSocketGrid();
    }

    void SpawnSocketGrid()
    {
        if (socketPrefab == null || dicePrefab == null)
        {
            return;
        }

        float socketHeightOffset = dicePrefab.transform.localScale.y / 2f;

        Vector3 dicePrefabScale = dicePrefab.transform.localScale;
        Vector3 targetColliderSize = dicePrefabScale;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xSpacing = targetColliderSize.x + socketGap;
                float ySpacing = targetColliderSize.z + socketGap; // Z-axis vì Unity dùng x-z cho mặt phẳng

                Vector3 spawnPosition = 
                    transform.position 
                    + new Vector3(col * xSpacing, socketHeightOffset, row * ySpacing)
                    - new Vector3(columns * xSpacing / 2f, 0, rows * ySpacing / 2f)
                ;

                InstantiateSocket(spawnPosition);
            }
        }
    }

    public GameObject InstantiateSocket(Vector3 spawnPosition) {
        GameObject socket = Instantiate(socketPrefab, spawnPosition, Quaternion.identity, transform);
        socket.GetComponent<SocketManager>().boardManager = this;
        socket.transform.localScale = dicePrefab.transform.localScale;
        return socket;
    }
}
