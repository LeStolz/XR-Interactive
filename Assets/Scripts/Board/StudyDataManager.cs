using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Main.BoardGameManager;

namespace Main
{
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
		List<RaySpaceDataPoint> raySpaceDataPoints = new();
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

			raySpaceDataPoints.Add(new(
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

			Debug.Log(currentCondition);
			Debug.Log(timeSinceStart);
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