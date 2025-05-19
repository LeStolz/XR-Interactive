using System;
using System.Collections.Generic;
using SFB;
using UnityEngine;

namespace Main
{
    [Serializable]
    struct UserStudyData
    {
        public List<RaySpaceDataPoint> raySpaceDataPoints;
    }

    [Serializable]
    struct RaySpaceDataPoint
    {
        public double endTimeStamp;
        public string raySpace;

        public RaySpaceDataPoint(double endTimeStamp, Tracker.RaySpace raySpace)
        {
            this.endTimeStamp = endTimeStamp;
            this.raySpace = raySpace.ToString();
        }
    }

    class StudyDataManager : MonoBehaviour
    {
        static StudyDataManager instance;
        DateTime startTime;
        UserStudyData userStudyData = new()
        {
            raySpaceDataPoints = new List<RaySpaceDataPoint>()
        };
        bool gameIsOngoing = false;
        string currentCondition;
        bool isZED = false;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            BoardGameManager.Instance.OnGameStatusChanged += HandleGameStatusChanged;

        }

        void OnDestroy()
        {
            BoardGameManager.Instance.OnGameStatusChanged -= HandleGameStatusChanged;
        }

        void Update()
        {
            if (!isZED) return;

            if (gameIsOngoing)
            {
                Debug.Log((DateTime.UtcNow - startTime).TotalSeconds);
            }
        }

        void GameStarted()
        {
            var zed = NetworkGameManager.Instance.FindPlayerByRole<ZEDModelManager>(Role.ZED);
            if (zed != null && zed.IsOwner)
            {
                isZED = true;
            }

            if (!isZED) return;

            gameIsOngoing = true;
            startTime = DateTime.UtcNow;
            currentCondition = $"{BoardGameManager.Instance.RayCastMode}_{DateTime.Now.ToString("MMdd_HHmm")}";

            var serverTracker = NetworkGameManager.Instance.FindPlayerByRole<ServerTrackerManager>(Role.ServerTracker);
            if (serverTracker != null)
            {
                foreach (var tracker in serverTracker.Trackers)
                {
                    tracker.OnRaySpaceChanged += HandleRaySpaceChanged;
                }
            }
        }

        Tracker.RaySpace currentRaySpace = Tracker.RaySpace.TabletDueToTrackerNotVisible;
        void HandleRaySpaceChanged(Tracker.RaySpace raySpace)
        {
            var currentTime = DateTime.UtcNow;
            var totalSecsSinceStart = (currentTime - startTime).TotalSeconds;
            Debug.Log($"{currentRaySpace}:{totalSecsSinceStart}");

            if (totalSecsSinceStart == 0)
            {
                return;
            }

            userStudyData.raySpaceDataPoints.Add(new(
                endTimeStamp: totalSecsSinceStart,
                raySpace: currentRaySpace
            ));
            currentRaySpace = raySpace;
        }

        void GameEnded()
        {
            if (!isZED) return;

            gameIsOngoing = false;

            var serverTracker = NetworkGameManager.Instance.FindPlayerByRole<ServerTrackerManager>(Role.ServerTracker);
            if (serverTracker != null)
            {
                foreach (var tracker in serverTracker.Trackers)
                {
                    tracker.OnRaySpaceChanged -= HandleRaySpaceChanged;
                }
            }
            HandleRaySpaceChanged(Tracker.RaySpace.TabletDueToTrackerNotVisible);

            var tablet = NetworkGameManager.Instance.FindPlayerByRole<TabletManager>(Role.Tablet);
            var tabletID = tablet == null ? "NULL" : tablet.PlayerName;
            var HMD = NetworkGameManager.Instance.FindPlayerByRole<XRINetworkAvatar>(Role.HMD);
            var HMDID = HMD == null ? "NULL" : HMD.PlayerName;

            var layoutID = BoardGameManager.Instance.CurrentBoardID;
            var fileName = $"HMD{HMDID}_Tablet{tabletID}_Layout{layoutID}_{currentCondition}";

            var filePath = StandaloneFileBrowser.SaveFilePanel(
                "Save User Study Data",
                null,
                fileName,
                "csv"
            );

            var csv = "sep=,\nEndTimeStamp,RaySpace\n";
            foreach (var dataPoint in userStudyData.raySpaceDataPoints)
            {
                csv += $"{dataPoint.endTimeStamp},{dataPoint.raySpace}\n";
            }
            System.IO.File.WriteAllText(filePath, csv);

            userStudyData = new UserStudyData
            {
                raySpaceDataPoints = new List<RaySpaceDataPoint>()
            };
            currentRaySpace = Tracker.RaySpace.TabletDueToTrackerNotVisible;
        }

        void HandleGameStatusChanged(BoardGameManager.GameStatus gameStatus)
        {
            switch (gameStatus)
            {
                case BoardGameManager.GameStatus.Started:
                    GameStarted();
                    break;
                case BoardGameManager.GameStatus.Won:
                    GameEnded();
                    break;
                default:
                    break;
            }
        }
    }
}
