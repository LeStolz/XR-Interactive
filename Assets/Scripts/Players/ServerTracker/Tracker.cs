using System;
using System.Linq;
using UnityEngine;

namespace Main
{
	class Tracker : MonoBehaviour
	{
		[SerializeField]
		GameObject arrow;
		HitMarker[] hitMarkers;
		[SerializeField]
		int id;

		public enum RayCastMode
		{
			None,
			Indirect,
			Hybrid
		}

		public enum RaySpace
		{
			None,
			PhysicalSpace,
			ScreenSpace,
		}

		RaySpace currentRaySpace = RaySpace.None;
		public Action<RaySpace> OnRaySpaceChanged;

		public void StartRayCastAndTeleport(Camera outputPortal)
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			RayCastAndTeleport(
				outputPortal,
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
			currentRaySpace = RaySpace.None;
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

			var raySpace =
				numBounce == -1 ? RaySpace.None :
				numBounce == 0 ? RaySpace.PhysicalSpace :
				RaySpace.ScreenSpace;

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

		void RayCastAndTeleport(Camera outputPortal, Ray ray, int depth)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 10, LayerMask.GetMask("Default")))
			{
				if (BoardGameManager.Instance.RayCastMode != RayCastMode.None)
				{
					hitMarkers[depth].transform.position = hit.point;
					hitMarkers[depth].transform.forward = hit.normal;
				}

				if (!hit.transform.gameObject.CompareTag("InputPortal"))
				{
					if (BoardGameManager.Instance.RayCastMode == RayCastMode.Indirect)
					{
						HideAllFromDepth(depth);
					}
					else
					{
						HideAllFromDepth(depth - 1);
					}
					return;
				}

				var inputPortal = hit.transform.gameObject.GetComponent<Portal>();

				RayCastAndTeleport(
					outputPortal,
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