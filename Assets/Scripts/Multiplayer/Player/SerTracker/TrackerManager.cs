using UnityEngine;

namespace Multiplayer
{
	class TrackerManager : MonoBehaviour
	{
		[SerializeField]
		Camera outputPortalCamera;
		[SerializeField]
		Hitmarker[] hitMarkers;
		[SerializeField]
		GameObject model;
		[SerializeField]
		GameObject arrow;
		[SerializeField]
		SerTrackerManager serTrackerManager;

		public void SetOutputPortal(GameObject outputPortal, int pixelWidth, int pixelHeight)
		{
			outputPortalCamera = outputPortal.GetComponent<Camera>();
			// outputPortalCamera.pixelWidth = pixelWidth;
			// outputPortalCamera.pixelHeight = pixelHeight;
			outputPortalCamera = outputPortal.GetComponent<Camera>();
		}

		void Update()
		{
			if (!serTrackerManager.IsLocalOwner)
			{
				return;
			}

			model.transform.SetPositionAndRotation(transform.position, transform.rotation);

			if (outputPortalCamera == null)
			{
				return;
			}

			RayCastAndTeleport(new Ray(arrow.transform.position, arrow.transform.forward), hitMarkers.Length - 1);
		}

		void RayCastAndTeleport(Ray ray, int depth)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Room")))
			{
				hitMarkers[depth].transform.position = hit.point;
				hitMarkers[depth].transform.forward = hit.normal;

				serTrackerManager.DrawLineRpc(
					depth,
					ray.origin, hitMarkers[depth].transform.position
				);

				if (!hit.transform.gameObject.CompareTag("InputPortal"))
				{
					return;
				}

				var inputPortal = hit.transform.gameObject.GetComponent<Portal>();

				RayCastAndTeleport(
					outputPortalCamera.ScreenPointToRay(inputPortal.PortalSpaceToScreenSpace(hit.point, outputPortalCamera)),
					depth - 1
				);
			}
			else
			{
				serTrackerManager.DrawLineRpc(
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