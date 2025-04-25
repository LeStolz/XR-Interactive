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
		public Tracker.RaySpace raySpace;

		public RaySpaceDataPoint(long endTimeStamp, Tracker.RaySpace raySpace)
		{
			this.endTimeStamp = endTimeStamp;
			this.raySpace = raySpace;
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
			if (!IsHost)
			{
				Destroy(this);
				return;
			}

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

			Debug.Log("Ray space changed: " + raySpace.ToString());

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
				"json"
			);
			var json = JsonUtility.ToJson(userStudyData, true);
			System.IO.File.WriteAllText(filePath, json);

			userStudyData = new UserStudyData
			{
				raySpaceDataPoints = new List<RaySpaceDataPoint>()
			};
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