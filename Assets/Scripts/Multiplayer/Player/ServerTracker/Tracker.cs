using System.Linq;
using UnityEngine;

namespace Multiplayer
{
	class Tracker : MonoBehaviour
	{
		[SerializeField]
		GameObject arrow;
		HitMarker[] hitMarkers;
		[SerializeField]
		int id;
		[SerializeField]
		ServerTrackerManager serverTrackerManager;

		public void StartRayCastAndTeleport(Camera outputPortal)
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			RayCastAndTeleport(
				outputPortal,
				new Ray(
					arrow.transform.position,
					arrow.transform.forward
				),
				hitMarkers.Length - 1
			);
		}

		void RayCastAndTeleport(Camera outputPortal, Ray ray, int depth)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Room")))
			{
				hitMarkers[depth].transform.position = hit.point;
				hitMarkers[depth].transform.forward = hit.normal;

				serverTrackerManager.DrawLineRpc(
					id, depth,
					ray.origin, hitMarkers[depth].transform.position
				);

				if (!hit.transform.gameObject.CompareTag("InputPortal"))
				{
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
				serverTrackerManager.DrawLineRpc(
					id, depth,
					ray.origin, ray.origin + ray.direction * 10
				);

				while (depth >= 0)
				{
					hitMarkers[depth].transform.position = new(0, -10, 0);
					depth--;
				}
			}
		}

		public void DrawLine(int hitMarkerId, Vector3 start, Vector3 end)
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			var lineRenderer = hitMarkers[hitMarkerId].GetComponent<LineRenderer>();

			if (start == end)
			{
				lineRenderer.enabled = false;
				return;
			}

			lineRenderer.enabled = true;
			lineRenderer.SetPosition(0, start);
			lineRenderer.SetPosition(1, end);
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

			var ZedModelManager = FindFirstObjectByType<ZEDModelManager>();

			if (ZedModelManager == null)
			{
				return false;
			}

			hitMarkers = ZedModelManager.HitMarkers.Skip(2 * id).Take(2).ToArray();

			return true;
		}
	}
}