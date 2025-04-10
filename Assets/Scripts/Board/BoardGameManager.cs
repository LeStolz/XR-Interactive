using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Main
{
    class BoardGameManager : NetworkBehaviour
    {
        public static BoardGameManager Instance { get; private set; }

        [field: SerializeField]
        public GameObject[] TilePrefabs { get; private set; }
        [field: SerializeField]
        public GameObject SocketPrefab { get; private set; }
        [SerializeField]
        GameObject confettiPrefab;
        [field: SerializeField]
        public GameObject AnswerBoardOrigin { get; private set; }

        [SerializeField]
        int numRows = 4;
        [SerializeField]
        int numCols = 4;

        [SerializeField]
        bool isTesting = false;

        public enum GameStatus
        {
            Started,
            Stopped,
            Won,
        }
        GameStatus gameStatus = GameStatus.Stopped;
        public Action<GameStatus> OnGameStatusChanged;

        readonly List<GameObject> tiles = new();
        readonly List<GameObject> tilesInSockets = new();
        readonly List<GameObject> answerTiles = new();
        readonly List<GameObject> sockets = new();
        BoardData boardData = default;

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
            transform.eulerAngles = new Vector3(0, 0, 0);
            AnswerBoardOrigin.transform.eulerAngles = new Vector3(0, 0, 0);
        }

        void Update()
        {
            if (NetworkGameManager.Instance.localRole != Role.ServerTracker)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveBoardData();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                isTesting = !isTesting;
            }
        }

        [Rpc(SendTo.Server)]
        public void StartGameRpc()
        {
            if (gameStatus == GameStatus.Won)
            {
                return;
            }

            if (gameStatus == GameStatus.Started)
            {
                StopGameRpc();
            }

            SpawnBoard();
            LoadBoardData();
            SpawnAnswerTiles();
            SpawnTiles();

            StartGameClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        void StartGameClientRpc()
        {
            gameStatus = GameStatus.Started;
            OnGameStatusChanged?.Invoke(gameStatus);
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

            foreach (GameObject answerTile in answerTiles)
            {
                answerTile.GetComponent<NetworkObject>().Despawn(true);
            }

            tiles.Clear();
            sockets.Clear();
            answerTiles.Clear();
            boardData = default;

            StopGameClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        void StopGameClientRpc()
        {
            gameStatus = GameStatus.Stopped;
            OnGameStatusChanged?.Invoke(gameStatus);
        }

        void SpawnBoard()
        {
            Vector3 socketScale = TilePrefabs[0].transform.localScale;

            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    float xSpacing = socketScale.x;
                    float zSpacing = socketScale.z;

                    Vector3 spawnPosition =
                        transform.position
                        + new Vector3(col * xSpacing, socketScale.y / 2f, row * zSpacing)
                        - new Vector3((numCols / 2f - 0.5f) * xSpacing, 0, (numRows / 2f - 0.5f) * zSpacing)
                    ;

                    var socketInstance = Instantiate(SocketPrefab, spawnPosition, Quaternion.identity);
                    socketInstance.GetComponent<NetworkObject>().Spawn(true);
                    sockets.Add(socketInstance);
                }
            }
        }

        void SaveBoardData()
        {
            TileData[] tileDatas = new TileData[tilesInSockets.Count];

            for (var i = 0; i < tilesInSockets.Count; i++)
            {
                var tile = tilesInSockets[i];

                TileData tileData = new(
                    tile.transform.position - transform.position,
                    tile.transform.eulerAngles,
                    int.Parse(tile.name)
                );
                tileDatas[i] = tileData;
            }

            BoardData boardData = new(tileDatas);
            string json = JsonUtility.ToJson(boardData, true);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/board.json", json);
            Debug.Log("Board saved to " + Application.persistentDataPath + "/board.json");
        }

        void LoadBoardData()
        {
            if (isTesting)
            {
                return;
            }

            string path = Application.persistentDataPath + "/board.json";

            if (!System.IO.File.Exists(path))
            {
                Debug.LogError("No saved board found at " + path);
                return;
            }

            string json = System.IO.File.ReadAllText(path);
            boardData = JsonUtility.FromJson<BoardData>(json);

            Debug.Log("Board loaded from " + path);
        }

        void SpawnAnswerTiles()
        {
            if (isTesting)
            {
                return;
            }

            foreach (TileData tileData in boardData.tiles)
            {
                GameObject prefab = TilePrefabs[tileData.prefabID];
                if (prefab != null)
                {
                    GameObject tile = Instantiate(
                        prefab,
                        AnswerBoardOrigin.transform.position + tileData.position,
                        Quaternion.Euler(tileData.eulerAngles)
                    );
                    tile.GetComponent<NetworkObject>().Spawn(true);
                    tile.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    tile.GetComponent<Tile>().SetTileIDRpc(tileData.prefabID.ToString());
                    answerTiles.Add(tile);
                }
            }

            answerTiles.Sort((a, b) => a.name.CompareTo(b.name));
        }

        void SpawnTiles()
        {
            List<int> tileIDsToSpawn;

            if (isTesting)
            {
                tileIDsToSpawn = new List<int>();
                for (int i = 0; i < TilePrefabs.Length * 2; i++)
                {
                    tileIDsToSpawn.Add(i % TilePrefabs.Length);
                }
            }
            else
            {
                tileIDsToSpawn = boardData.tiles.Select(t => t.prefabID).ToList();
            }

            foreach (var tileID in tileIDsToSpawn)
            {
                var tilePrefab = TilePrefabs[tileID];

                var distanceFromCenter = UnityEngine.Random.Range(
                    1f, 2f
                ) + numRows * tilePrefab.transform.localScale.z / 2f;
                var angleFromCenter = UnityEngine.Random.Range(0f, 360f);

                var x = distanceFromCenter * Mathf.Cos(angleFromCenter * Mathf.Deg2Rad);
                var z = distanceFromCenter * Mathf.Sin(angleFromCenter * Mathf.Deg2Rad);
                var y = tilePrefab.transform.localScale.y / 1.5f;

                var pos = new Vector3(x, y, z) + transform.position;
                var rot = Quaternion.Euler(
                    UnityEngine.Random.Range(0, 360),
                    UnityEngine.Random.Range(0, 360),
                    UnityEngine.Random.Range(0, 360)
                );

                GameObject tile = Instantiate(tilePrefab, pos, rot);
                tile.GetComponent<NetworkObject>().Spawn(true);
                tile.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                tile.GetComponent<Tile>().SetTileIDRpc(tileID.ToString());
                tiles.Add(tile);
            }
        }

        public void AttachTileToSocket(GameObject tile)
        {
            if (tilesInSockets.Contains(tile))
            {
                return;
            }

            tilesInSockets.Add(tile);

            StartCoroutine(CheckWinCondition(tile.transform.position - transform.position));
        }

        public void DetachTileFromSocket(GameObject tile)
        {
            if (!tilesInSockets.Contains(tile))
            {
                return;
            }

            tilesInSockets.Remove(tile);
        }

        IEnumerator CheckWinCondition(Vector3 lastAttachedPosition)
        {
            if (!IsWon())
            {
                yield break;
            }

            WonRpc(lastAttachedPosition);

            yield return new WaitForSeconds(2 * confettiPrefab.GetComponent<ParticleSystem>().main.duration);

            StopGameRpc();
        }

        IEnumerator SpawnConfetti(Vector3 lastAttachedPosition)
        {
            GameObject answerConfetti = null;
            var localRole = NetworkGameManager.Instance.localRole;
            if (localRole == Role.Tablet || localRole == Role.ServerTracker)
            {
                answerConfetti = Instantiate(
                    confettiPrefab,
                    AnswerBoardOrigin.transform.position + lastAttachedPosition,
                    Quaternion.identity
                );
            }
            var confetti = Instantiate(
                confettiPrefab,
                transform.position + lastAttachedPosition,
                Quaternion.identity
            );

            yield return new WaitForSeconds(2 * confettiPrefab.GetComponent<ParticleSystem>().main.duration);

            if (answerConfetti != null) Destroy(answerConfetti);
            Destroy(confetti);
        }

        [Rpc(SendTo.Everyone)]
        void WonRpc(Vector3 lastAttachedPosition)
        {
            StartCoroutine(SpawnConfetti(lastAttachedPosition));

            gameStatus = GameStatus.Won;
            OnGameStatusChanged?.Invoke(gameStatus);
        }

        bool IsWon()
        {
            if (tilesInSockets.Count != answerTiles.Count)
            {
                return false;
            }

            tilesInSockets.Sort((a, b) => a.name.CompareTo(b.name));

            for (int i = 0; i < tilesInSockets.Count; i++)
            {
                if (tilesInSockets[i].name != answerTiles[i].name)
                {
                    return false;
                }

                var ansPos = answerTiles[i].transform.position - AnswerBoardOrigin.transform.position;
                var tilePos = tilesInSockets[i].transform.position - transform.position;
                var ansRot = answerTiles[i].transform.rotation;
                var tileRot = tilesInSockets[i].transform.rotation;

                Debug.Log(
                    $"ansPos: {ansPos}, tilePos: {tilePos}, ansRot: {ansRot}, tileRot: {tileRot}"
                );

                if (Vector3.Distance(ansPos, tilePos) > 0.1f || Quaternion.Angle(ansRot, tileRot) > 10f)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    struct BoardData
    {
        public TileData[] tiles;

        public BoardData(TileData[] tiles)
        {
            this.tiles = tiles;
        }
    }

    [Serializable]
    struct TileData
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