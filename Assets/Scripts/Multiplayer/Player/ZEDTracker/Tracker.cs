using System;
using UnityEngine;

namespace Multiplayer
{
	class Tracker : MonoBehaviour
	{
		[SerializeField]
		GameObject arrow;
		[SerializeField]
		HitMarker[] hitMarkers;

		public void StartRayCastAndTeleport(Camera outputPortal, Action<int, Vector3, Vector3> DrawLineRpc)
		{
			RayCastAndTeleport(
				outputPortal,
				new Ray(
					arrow.transform.position,
					arrow.transform.forward
				),
				hitMarkers.Length - 1,
				DrawLineRpc
			);
		}

		void RayCastAndTeleport(Camera outputPortal, Ray ray, int depth, Action<int, Vector3, Vector3> DrawLineRpc)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Room")))
			{
				hitMarkers[depth].transform.position = hit.point;
				hitMarkers[depth].transform.forward = hit.normal;

				DrawLineRpc(
					depth,
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
					depth - 1,
					DrawLineRpc
				);
			}
			else
			{
				DrawLineRpc(
					depth,
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
	}
}