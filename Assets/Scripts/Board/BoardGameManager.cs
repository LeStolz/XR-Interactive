using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Main
{
    class BoardGameManager : NetworkBehaviour
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

        readonly List<GameObject> tiles = new();
        readonly List<GameObject> sockets = new();

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
            SpawnBoard();

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

                        var distanceFromCenter = UnityEngine.Random.Range(
                            1f, 2f
                        ) + numRows * tile.transform.localScale.z / 2f;
                        var angleFromCenter = UnityEngine.Random.Range(0f, 360f);
                        var x = distanceFromCenter * Mathf.Cos(angleFromCenter * Mathf.Deg2Rad);
                        var z = distanceFromCenter * Mathf.Sin(angleFromCenter * Mathf.Deg2Rad);
                        var y = tile.transform.localScale.y / 1.9f;
                        var pos = new Vector3(x, y, z) + transform.position;
                        var rot = Quaternion.Euler(
                            UnityEngine.Random.Range(0, 360),
                            UnityEngine.Random.Range(0, 360),
                            UnityEngine.Random.Range(0, 360)
                        );

                        GameObject tileInstance = Instantiate(tile, pos, rot);
                        tileInstance.GetComponent<NetworkObject>().Spawn(true);
                        tileInstance.GetComponent<Tile>().SetTileIDRpc(tileID.ToString());
                        tiles.Add(tileInstance);
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
            foreach (GameObject tile in tiles)
            {
                tile.GetComponent<NetworkObject>().Despawn(true);
            }

            foreach (GameObject socket in sockets)
            {
                socket.GetComponent<NetworkObject>().Despawn(true);
            }

            tiles.Clear();
            sockets.Clear();

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
                    spawnPosition += transform.position;

                    var socketInstance = Instantiate(socketPrefab, spawnPosition, Quaternion.identity);
                    socketInstance.GetComponent<NetworkObject>().Spawn(true);
                    sockets.Add(socketInstance);
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
}