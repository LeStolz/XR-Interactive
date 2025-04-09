using System;
using Main;
using Unity.Netcode;
using UnityEngine;

public class BoardGameManager : NetworkBehaviour
{
    [SerializeField]
    GameObject[] tilePrefabs;
    [SerializeField]
    GameObject socketPrefab;
    [field: SerializeField]
    public GameObject AnswerBoardOrigin { get; private set; }

    public static BoardGameManager Instance { get; private set; }
    public static float socketGap = 0.01f;

    public int rows = 4;
    public int columns = 4;

    public bool IsPlaying { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SpawnBoard();
    }

    void Update()
    {
        if (NetworkGameManager.Instance.localRole != Role.ServerTracker)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveBoard();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            LoadBoard();
        }
    }

    public void StartGame()
    {
        IsPlaying = true;
    }

    public void StopGame()
    {
        IsPlaying = false;
    }


    void SpawnBoard()
    {
        if (socketPrefab == null || tilePrefabs == null)
        {
            return;
        }

        float socketHeightOffset = tilePrefabs[0].transform.localScale.y / 2f;
        Vector3 socketScale = tilePrefabs[0].transform.localScale;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xSpacing = socketScale.x + socketGap;
                float ySpacing = socketScale.z + socketGap; // Z-axis vì Unity dùng x-z cho mặt phẳng

                Vector3 spawnPosition =
                    transform.position
                    + new Vector3(col * xSpacing, socketHeightOffset, row * ySpacing)
                    - new Vector3(columns * xSpacing / 2f, 0, rows * ySpacing / 2f)
                ;

                InstantiateSocket(spawnPosition);
            }
        }
    }

    public GameObject InstantiateSocket(Vector3 spawnPosition)
    {
        GameObject socket = Instantiate(socketPrefab, spawnPosition, Quaternion.identity, transform);
        socket.GetComponent<SocketManager>().boardManager = this;
        socket.transform.localScale = tilePrefabs[0].transform.localScale;
        return socket;
    }

    void SaveBoard()
    {
        TileData[] tiles = new TileData[transform.childCount];

        foreach (Transform child in transform)
        {
            TileData tileData = new(
                child.position, child.eulerAngles, child.localScale, int.Parse(child.name)
            );
            tiles[child.GetSiblingIndex()] = tileData;
        }

        BoardData boardData = new(tiles);
        string json = JsonUtility.ToJson(boardData, true);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/board.json", json);
        Debug.Log("Board saved to " + Application.persistentDataPath + "/board.json");
    }

    void LoadBoard()
    {
        string path = Application.persistentDataPath + "/board.json";

        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("No saved board found at " + path);
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        BoardData boardData = JsonUtility.FromJson<BoardData>(json);

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        foreach (TileData tileData in boardData.tiles)
        {
            GameObject prefab = tilePrefabs[tileData.prefabID];
            if (prefab != null)
            {
                GameObject tile = Instantiate(prefab, tileData.position, Quaternion.Euler(tileData.eulerAngles), transform);
                tile.name = tileData.prefabID.ToString();
                tile.transform.localScale = tileData.scale;
            }
        }

        Debug.Log("Board loaded from " + path);
    }
}

[Serializable]
public struct BoardData
{
    public TileData[] tiles;

    public BoardData(TileData[] tiles)
    {
        this.tiles = tiles;
    }
}

[Serializable]
public struct TileData
{
    public Vector3 position;
    public Vector3 eulerAngles;
    public Vector3 scale;

    public int prefabID;

    public TileData(Vector3 position, Vector3 eulerAngles, Vector3 scale, int prefabID)
    {
        this.position = position;
        this.eulerAngles = eulerAngles;
        this.scale = scale;
        this.prefabID = prefabID;
    }
}