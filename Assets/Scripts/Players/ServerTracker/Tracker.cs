using System;
using System.Linq;
using UnityEngine;

namespace Main
{
	class Tracker : MonoBehaviour
	{
		[SerializeField]
		bool enableFishingRodPointing = false;
		[SerializeField]
		GameObject arrow;
		[SerializeField]
		int id;
		[SerializeField]
		ServerTrackerManager serverTrackerManager;
		HitMarker[] hitMarkers;
		Camera outputPortal;

		public enum RayCastMode
		{
			None,
			Indirect,
			Hybrid
		}

		public enum RaySpace
		{
			PhysicalSpace,
			ObjectInPhysicalSpace,
			ScreenSpace,
			ObjectInScreenSpace
		}

		RaySpace currentRaySpace = RaySpace.PhysicalSpace;
		public string rayHitTag = "";
		public Action<RaySpace> OnRaySpaceChanged;

		public void StartRayCastAndTeleport(Camera outputPortal)
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			rayHitTag = "";
			this.outputPortal = outputPortal;

			RayCastAndTeleport(
				new Ray(arrow.transform.position, arrow.transform.forward),
				hitMarkers.Length - 1
			);
		}

		void Start()
		{
			BoardGameManager.Instance.OnGameStatusChanged += OnGameStatusChanged;
		}

		void OnDestroy()
		{
			BoardGameManager.Instance.OnGameStatusChanged -= OnGameStatusChanged;
		}

		void OnGameStatusChanged(BoardGameManager.GameStatus gameStatus)
		{
			currentRaySpace = RaySpace.PhysicalSpace;
			OnRaySpaceChanged?.Invoke(currentRaySpace);
		}

		void Update()
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			var numBounce = hitMarkers.Count(hm => hm.IsShowing()) - 1;

			for (int i = 0; i < hitMarkers.Length; i++)
			{
				if (!hitMarkers[i].IsShowing())
				{
					hitMarkers[i].Hide();
					continue;
				}

				hitMarkers[i].Show(
					i, numBounce, arrow.transform.position, arrow.transform.forward, hitMarkers[i].transform.position
				);
			}

			RaySpace raySpace = RaySpace.PhysicalSpace;
			if (numBounce == 0)
			{
				raySpace = rayHitTag == "StudyObject" ? RaySpace.ObjectInPhysicalSpace : RaySpace.PhysicalSpace;
			}
			else if (numBounce == 1)
			{
				raySpace = rayHitTag == "StudyObject" ? RaySpace.ObjectInScreenSpace : RaySpace.ScreenSpace;
			}

			if (currentRaySpace != raySpace)
			{
				currentRaySpace = raySpace;
				OnRaySpaceChanged?.Invoke(raySpace);
			}
		}

		void HideAllFromDepth(int depth)
		{
			while (depth >= 0)
			{
				hitMarkers[depth].Hide();
				depth--;
			}
		}

		void RayCastAndTeleport(Ray ray, int depth)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 10, LayerMask.GetMask("Default")))
			{
				serverTrackerManager.UpdateRayHitTagRpc(id, hit.transform.gameObject.tag);

				hitMarkers[depth].ToggleVisiblity(BoardGameManager.Instance.RayCastMode != RayCastMode.None);
				hitMarkers[depth].transform.position = hit.point;
				hitMarkers[depth].transform.forward = hit.normal;

				if (enableFishingRodPointing && hit.transform.gameObject.CompareTag("Ceiling"))
				{
					RayCastAndTeleport(
						new(hit.point, hit.normal),
						depth - 1
					);

					return;
				}

				if (!hit.transform.gameObject.CompareTag("InputPortal"))
				{
					hitMarkers[depth].ToggleVisiblity(
						!(depth > 0 && BoardGameManager.Instance.RayCastMode == RayCastMode.Indirect)
					);
					HideAllFromDepth(depth - 1);
					return;
				}

				var inputPortal = hit.transform.gameObject.GetComponent<Portal>();

				RayCastAndTeleport(
					outputPortal.ScreenPointToRay(inputPortal.PortalSpaceToScreenSpace(hit.point, outputPortal)),
					depth - 1
				);
			}
			else
			{
				HideAllFromDepth(depth);
			}
		}

		bool UpdateHitmarkers()
		{
			if (
				hitMarkers != null &&
				hitMarkers.Length != 0 &&
				hitMarkers.All(hm => hm != null)
			)
			{
				return true;
			}

			var ZedModelManager = NetworkGameManager.Instance.FindPlayerByRole<ZEDModelManager>(Role.ZED);

			if (ZedModelManager == null)
			{
				return false;
			}

			hitMarkers = ZedModelManager.HitMarkers.Skip(2 * id).Take(2).ToArray();

			return true;
		}
	}
}