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
        public GameObject[] TilesPrefabs { get; private set; }
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

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.S))
            {
                SkipBoard();
            }
#endif
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
            sockets = Instantiate(SocketsPrefab, transform.position, Quaternion.identity);
        }

        void SkipBoard()
        {
            var nextBoardID = (PlayerPrefs.GetInt("CurrentBoardID", 0) + 1) % TilesPrefabs.Length;
            PlayerPrefs.SetInt("CurrentBoardID", nextBoardID);
            PlayerPrefs.Save();

            PlayerHudNotification.Instance.ShowText("Current board ID: " + nextBoardID);
            Debug.Log("Current board ID: " + nextBoardID);
        }

        void SpawnAnswerTiles()
        {
            var currentBoardID = PlayerPrefs.GetInt("CurrentBoardID", 0);
            var tilesPrefab = TilesPrefabs[currentBoardID];

            foreach (Transform tilePrefabTransform in tilesPrefab.transform)
            {
                var tilePrefab = tilePrefabTransform.gameObject;
                GameObject tile = Instantiate(
                    tilePrefab,
                    AnswerBoardOrigin.transform.position + tilePrefab.transform.position,
                    Quaternion.Euler(tilePrefab.transform.eulerAngles)
                );
                tile.GetComponent<NetworkObject>().Spawn(true);
                tile.GetComponent<Tile>().SetupRpc(
                    AnswerBoardOrigin.transform.position + tilePrefab.transform.position,
                    tilePrefab.transform.eulerAngles, tilePrefab.name.ToString(), true
                );
                answerTiles.Add(tile);
            }

            answerTiles.Sort((a, b) => a.name.CompareTo(b.name));
        }

        void SpawnTiles(ulong hmdPlayerId)
        {
            var currentBoardID = PlayerPrefs.GetInt("CurrentBoardID", 0);
            var tilesPrefab = TilesPrefabs[currentBoardID];

            foreach (Transform tilePrefab in tilesPrefab.transform)
            {
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

                GameObject tile = Instantiate(tilePrefab.gameObject, pos, rot);
                tile.GetComponent<NetworkObject>().SpawnWithOwnership(hmdPlayerId, true);
                tile.GetComponent<Tile>().SetupRpc(
                    pos, rot.eulerAngles, tilePrefab.ToString(), false
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
            SkipBoard();

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

                if (Vector3.Distance(ansPos, tilePos) > 0.1f || Quaternion.Angle(ansRot, tileRot) > 10f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}