using System;
using Main;
using Unity.Netcode;
using UnityEngine;

public class BoardGameManager : NetworkBehaviour
{
    [SerializeField]
    bool isTesting = false;

    [SerializeField]
    int numTiles = 8;

    [field: SerializeField]
    public GameObject[] TilePrefabs { get; private set; }
    [SerializeField]
    GameObject socketPrefab;
    [field: SerializeField]
    public GameObject AnswerBoardOrigin { get; private set; }

    public static BoardGameManager Instance { get; private set; }

    public int numRows = 4;
    public int numCols = 4;

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
        if (IsServer)
        {
            SpawnBoard();
        }
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
    }

    [Rpc(SendTo.Server)]
    public void StartGameRpc()
    {
        if (isTesting)
        {
            int currentNumTiles = 0;
            while (currentNumTiles < numTiles)
            {
                for (var tileID = 0; tileID < TilePrefabs.Length; tileID++)
                {
                    var tile = TilePrefabs[tileID];

                    if (currentNumTiles >= numTiles)
                    {
                        break;
                    }

                    var signX = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
                    var signZ = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
                    var posX = UnityEngine.Random.Range(numCols, 1.5f * numCols) * signX * tile.transform.localScale.x;
                    var posZ = UnityEngine.Random.Range(numRows, 1.5f * numRows) * signZ * tile.transform.localScale.z;
                    var posY = 2f;
                    var pos = new Vector3(posX, posY, posZ);
                    var rot = Quaternion.Euler(
                        UnityEngine.Random.Range(0, 360),
                        UnityEngine.Random.Range(0, 360),
                        UnityEngine.Random.Range(0, 360)
                    );

                    GameObject tileInstance = Instantiate(tile, pos, rot, transform);
                    tileInstance.GetComponent<NetworkObject>().Spawn(true);
                    tileInstance.GetComponent<Tile>().SetTileIDRpc(tileID.ToString());
                    currentNumTiles++;
                }
            }
        }
        else
        {
            LoadBoard();
        }

        StartGameClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    void StartGameClientRpc()
    {
        IsPlaying = true;
    }

    [Rpc(SendTo.Server)]
    public void StopGameRpc()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        StopGameClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    void StopGameClientRpc()
    {
        IsPlaying = false;
    }

    void SpawnBoard()
    {
        if (socketPrefab == null || TilePrefabs == null)
        {
            return;
        }

        float socketHeightOffset = TilePrefabs[0].transform.localScale.y / 2f;
        Vector3 socketScale = TilePrefabs[0].transform.localScale;

        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                float xSpacing = socketScale.x;
                float ySpacing = socketScale.z;

                Vector3 spawnPosition =
                    transform.position
                    + new Vector3(col * xSpacing, socketHeightOffset, row * ySpacing)
                    - new Vector3(numCols * xSpacing / 2f, 0, numRows * ySpacing / 2f)
                ;

                var socketInstance = Instantiate(socketPrefab, spawnPosition, Quaternion.identity, transform);
                socketInstance.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    void SaveBoard()
    {
        TileData[] tiles = new TileData[transform.childCount];

        foreach (Transform child in transform)
        {
            TileData tileData = new(
                child.position, child.eulerAngles, int.Parse(child.name)
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
            GameObject prefab = TilePrefabs[tileData.prefabID];
            if (prefab != null)
            {
                GameObject tile = Instantiate(prefab, tileData.position, Quaternion.Euler(tileData.eulerAngles), transform);
                tile.GetComponent<NetworkObject>().Spawn(true);
                tile.GetComponent<Tile>().SetTileIDRpc(tileData.prefabID.ToString());
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

    public int prefabID;

    public TileData(Vector3 position, Vector3 eulerAngles, int prefabID)
    {
        this.position = position;
        this.eulerAngles = eulerAngles;
        this.prefabID = prefabID;
    }
}