using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SFB;
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
        public GameObject SocketsPrefab { get; private set; }
        [field: SerializeField]
        public GameObject SocketPrefab { get; private set; }
        [SerializeField]
        GameObject confettiPrefab;
        [field: SerializeField]
        public GameObject AnswerBoardOrigin { get; private set; }

        [SerializeField]
        Vector2 borderXMinMax = new(-1.5f, 1.5f);
        [SerializeField]
        Vector2 borderZMinMax = new(-2.5f, 2.5f);

        public bool isTesting = false;
        public int rayTeleportDepth = 0;

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
        GameObject sockets;
        BoardData boardData = default;

        [Rpc(SendTo.Everyone)]
        public void SetRayTeleportDepthRpc(int depth)
        {
            rayTeleportDepth = depth;
        }

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
                PlayerHudNotification.Instance.ShowText("Board saved");
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                isTesting = !isTesting;
                Debug.Log(isTesting);
                PlayerHudNotification.Instance.ShowText("Testing mode: " + (isTesting ? "ON" : "OFF"));
            }
        }

        [Rpc(SendTo.Server)]
        public void StartGameRpc()
        {
            if (gameStatus == GameStatus.Won)
            {
                return;
            }

            var hmdPlayer = NetworkGameManager.Instance.FindPlayerByRole<NetworkPlayer>(Role.HMD);
            ulong hmdPlayerId = hmdPlayer == null ? 0 : hmdPlayer.OwnerClientId;

            if (gameStatus == GameStatus.Started)
            {
                StopGameRpc();
            }

            SpawnBoardRpc(RpcTarget.Single(hmdPlayerId, RpcTargetUse.Temp));
            LoadBoardData();
            SpawnAnswerTiles();
            SpawnTiles(hmdPlayerId);

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

            foreach (GameObject answerTile in answerTiles)
            {
                answerTile.GetComponent<NetworkObject>().Despawn(true);
            }

            tiles.Clear();
            tilesInSockets.Clear();
            answerTiles.Clear();
            boardData = default;

            StopGameClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        void StopGameClientRpc()
        {
            gameStatus = GameStatus.Stopped;
            OnGameStatusChanged?.Invoke(gameStatus);

            if (NetworkGameManager.Instance.localRole == Role.HMD)
            {
                Destroy(sockets);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void SpawnBoardRpc(RpcParams rpcParams = default)
        {
            Vector3 socketScale = TilePrefabs[0].transform.localScale;
            GameObject socketContainer = Instantiate(SocketsPrefab, transform.position, Quaternion.identity);

            foreach (Transform socket in socketContainer.transform)
            {
                socket.position += new Vector3(
                    Mathf.Abs(socket.localPosition.x) > 0.01f ? -Mathf.Sign(socket.localPosition.x) * socketScale.x / 2f : 0,
                    socketScale.y / 2f,
                    Mathf.Abs(socket.localPosition.z) > 0.01f ? -Mathf.Sign(socket.localPosition.z) * socketScale.z / 2f : 0
                );
            }

            sockets = socketContainer;
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

            var path = StandaloneFileBrowser.SaveFilePanel("Save board", "", "", "json");
            path = path.Length == 0 ? Application.persistentDataPath + "/board.json" : path;

            System.IO.File.WriteAllText(path, json);
            Debug.Log("Board saved to " + path);
        }

        void LoadBoardData()
        {
            if (isTesting)
            {
                return;
            }

            var paths = StandaloneFileBrowser.OpenFilePanel("Load board", "", "json", false);
            string path = paths.Length == 0 ? Application.persistentDataPath + "/board.json" : paths[0];

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
                    tile.GetComponent<Tile>().SetupRpc(
                        AnswerBoardOrigin.transform.position + tileData.position,
                        tileData.eulerAngles, tileData.prefabID.ToString(), true
                    );
                    answerTiles.Add(tile);
                }
            }

            answerTiles.Sort((a, b) => a.name.CompareTo(b.name));
        }

        void SpawnTiles(ulong hmdPlayerId)
        {
            List<int> tileIDsToSpawn;

            if (isTesting)
            {
                tileIDsToSpawn = new List<int>();
                for (int i = 0; i < TilePrefabs.Length; i++)
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
                var borderZMinMaxOffset = new Vector2(
                    borderZMinMax.x + tilePrefab.transform.localScale.z / 2f,
                    borderZMinMax.y - tilePrefab.transform.localScale.z / 2f
                );
                var borderXMinMaxOffset = new Vector2(
                    borderXMinMax.x + tilePrefab.transform.localScale.x / 2f,
                    borderXMinMax.y - tilePrefab.transform.localScale.x / 2f
                );

                var x = UnityEngine.Random.Range(borderXMinMaxOffset.x, borderXMinMaxOffset.y);
                var z = UnityEngine.Random.Range(borderZMinMaxOffset.x, borderZMinMaxOffset.y);
                var y = tilePrefab.transform.localScale.y / 1.5f;

                var pos = new Vector3(x, y, z) + transform.position;
                var rot = Quaternion.Euler(
                    UnityEngine.Random.Range(0, 360),
                    UnityEngine.Random.Range(0, 360),
                    UnityEngine.Random.Range(0, 360)
                );

                GameObject tile = Instantiate(tilePrefab, pos, rot);
                tile.GetComponent<NetworkObject>().SpawnWithOwnership(hmdPlayerId, true);
                tile.GetComponent<Tile>().SetupRpc(
                    pos, rot.eulerAngles, tileID.ToString(), false
                );
                tiles.Add(tile);
            }
        }

        [Rpc(SendTo.Server)]
        public void AttachTileToSocketRpc(string tileName, Vector3 socketPos, Vector3 socketRot)
        {
            var tile = tiles.Find(t => t.name == tileName);

            if (tile == null)
            {
                return;
            }

            if (!tilesInSockets.Contains(tile))
            {
                tilesInSockets.Add(tile);
            }

            StartCoroutine(CheckWinCondition(tile, socketPos, socketRot));
        }

        [Rpc(SendTo.Server)]
        public void DetachTileFromSocketRpc(string tileName)
        {
            var tile = tiles.Find(t => t.name == tileName);

            if (tile == null)
            {
                return;
            }

            if (!tilesInSockets.Contains(tile))
            {
                return;
            }

            tilesInSockets.Remove(tile);
        }

        IEnumerator CheckWinCondition(GameObject tile, Vector3 socketPos, Vector3 socketRot)
        {
            yield return new WaitUntil(
                () =>
                    Vector3.Distance(tile.transform.position, socketPos) < 0.05f &&
                    Quaternion.Angle(tile.transform.rotation, Quaternion.Euler(socketRot)) < 5f,
                new TimeSpan(0, 0, 10), () => Debug.Log("Timed out")
            );

            if (!IsWon())
            {
                yield break;
            }

            WonRpc(tile.transform.position - transform.position);

            yield return new WaitForSeconds(2 * confettiPrefab.GetComponentInChildren<ParticleSystem>().main.duration);

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

            yield return new WaitForSeconds(2 * confettiPrefab.GetComponentInChildren<ParticleSystem>().main.duration);

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
            Debug.Log(tilesInSockets.Count + " == " + answerTiles.Count);

            if (isTesting || tilesInSockets.Count != answerTiles.Count)
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