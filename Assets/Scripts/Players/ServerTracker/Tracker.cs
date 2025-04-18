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

		public void StartRayCastAndTeleport(Camera outputPortal)
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			if (BoardGameManager.Instance.rayTeleportDepth <= 0)
			{
				HideAllFromDepth(hitMarkers.Length - 1);
				return;
			}

			RayCastAndTeleport(
				outputPortal,
				new Ray(arrow.transform.position, arrow.transform.forward),
				BoardGameManager.Instance.rayTeleportDepth - 1
			);
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
				hitMarkers[depth].transform.position = hit.point;
				hitMarkers[depth].transform.forward = hit.normal;

				if (!hit.transform.gameObject.CompareTag("InputPortal"))
				{
					HideAllFromDepth(depth - 1);
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