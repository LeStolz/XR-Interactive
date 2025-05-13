using System;
using System.Collections.Generic;
using SFB;
using Unity.Netcode;
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
		public long endTimeStamp;
		public string raySpace;

		public RaySpaceDataPoint(long endTimeStamp, Tracker.RaySpace raySpace)
		{
			this.endTimeStamp = endTimeStamp;
			this.raySpace = raySpace.ToString();
		}
	}

	class StudyDataManager : NetworkBehaviour
	{
		static StudyDataManager instance;
		long timeSinceStart = 0;
		UserStudyData userStudyData = new UserStudyData
		{
			raySpaceDataPoints = new List<RaySpaceDataPoint>()
		};
		bool gameIsOngoing = false;
		string currentCondition;

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

		public override void OnDestroy()
		{
			base.OnDestroy();
			BoardGameManager.Instance.OnGameStatusChanged -= HandleGameStatusChanged;
		}

		void Update()
		{
			if (!IsHost) return;

			if (gameIsOngoing)
			{
				timeSinceStart += (long)(Time.deltaTime * 1000);
			}
		}

		void GameStarted()
		{
			if (!IsHost) return;

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

		Tracker.RaySpace currentRaySpace = Tracker.RaySpace.TabletDueToTrackerNotVisible;
		void HandleRaySpaceChanged(Tracker.RaySpace raySpace)
		{
			Debug.Log($"{currentRaySpace}:{timeSinceStart}");

			if (timeSinceStart == 0)
			{
				return;
			}

			userStudyData.raySpaceDataPoints.Add(new(
				endTimeStamp: timeSinceStart,
				raySpace: currentRaySpace
			));
			currentRaySpace = raySpace;
		}

		void GameEnded()
		{
			if (!IsHost) return;

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
				case BoardGameManager.GameStatus.Stopped:
					GameEnded();
					break;
				default:
					break;
			}
		}
	}
}