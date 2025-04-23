using System;
using System.Collections.Generic;
using SFB;
using Unity.Netcode;
using UnityEngine;
using static Main.BoardGameManager;

namespace Main
{
	struct UserStudyData
	{
		public List<RaySpaceDataPoint> raySpaceDataPoints;
	}

	struct RaySpaceDataPoint
	{
		public long endTimeStamp;
		public Tracker.RaySpace raySpace;

		public RaySpaceDataPoint(long endTimeStamp, Tracker.RaySpace raySpace)
		{
			this.endTimeStamp = endTimeStamp;
			this.raySpace = raySpace;
		}
	}

	class StudyDataManager : NetworkBehaviour
	{
		public static StudyDataManager Instance { get; private set; }
		long timeSinceStart = 0;
		UserStudyData userStudyData = new UserStudyData
		{
			raySpaceDataPoints = new List<RaySpaceDataPoint>()
		};
		bool gameIsOngoing = false;
		string currentCondition;

		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else if (Instance != this)
			{
				Destroy(gameObject);
			}
			DontDestroyOnLoad(gameObject);
		}

		void Start()
		{
			BoardGameManager.Instance.OnGameStatusChanged += HandleGameStatusChanged;
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			BoardGameManager.Instance.OnGameStatusChanged -= HandleGameStatusChanged;
		}

		void Update()
		{
			if (!IsServer) return;

			if (gameIsOngoing)
			{
				timeSinceStart += (long)(Time.deltaTime * 1000);
			}
		}

		void GameStarted()
		{
			gameIsOngoing = true;

			timeSinceStart = 0;
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

		void HandleRaySpaceChanged(Tracker.RaySpace raySpace)
		{
			if (!gameIsOngoing) return;

			userStudyData.raySpaceDataPoints.Add(new(
				endTimeStamp: timeSinceStart,
				raySpace: raySpace
			));
		}

		void GameEnded()
		{
			gameIsOngoing = false;

			var serverTracker = NetworkGameManager.Instance.FindPlayerByRole<ServerTrackerManager>(Role.ServerTracker);
			if (serverTracker != null)
			{
				foreach (var tracker in serverTracker.Trackers)
				{
					tracker.OnRaySpaceChanged -= HandleRaySpaceChanged;
				}
			}

			var tabletUserID = NetworkGameManager.Instance.FindPlayerByRole<TabletManager>(Role.Tablet).PlayerName;
			var hmdUserID = NetworkGameManager.Instance.FindPlayerByRole<NetworkPlayer>(Role.HMD).PlayerName;
			var layoutID = BoardGameManager.Instance.CurrentBoardID;
			var fileName = $"HMD{hmdUserID}_Tablet{tabletUserID}_Layout{layoutID}_{currentCondition}";

			var filePath = StandaloneFileBrowser.SaveFilePanel(
				"Save User Study Data",
				null,
				fileName,
				"json"
			);
			var json = JsonUtility.ToJson(userStudyData, true);
			System.IO.File.WriteAllText(filePath, json);

			userStudyData = new UserStudyData
			{
				raySpaceDataPoints = new List<RaySpaceDataPoint>()
			};
		}

		void HandleGameStatusChanged(GameStatus gameStatus)
		{
			switch (gameStatus)
			{
				case GameStatus.Started:
					GameStarted();
					break;
				case GameStatus.Won:
					GameEnded();
					break;
				default:
					break;
			}
		}
	}
}