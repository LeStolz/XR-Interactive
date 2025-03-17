using UnityEngine;

namespace Multiplayer
{
	class TrackerManager : MonoBehaviour
	{
		[SerializeField]
		GameObject outputPortal;
		[SerializeField]
		Hitmarker[] hitMarkers;
		[SerializeField]
		GameObject model;
		[SerializeField]
		GameObject arrow;
		[SerializeField]
		SerTrackerManager serTrackerManager;

		public void SetOutputPortal(GameObject outputPortal)
		{
			this.outputPortal = outputPortal;
		}

		void Update()
		{
			if (!serTrackerManager.IsLocalOwner)
			{
				return;
			}

			model.transform.SetPositionAndRotation(transform.position, transform.rotation);

			if (outputPortal == null)
			{
				return;
			}

			RayCastAndTeleport(arrow.transform, hitMarkers.Length - 1);
		}

		void RayCastAndTeleport(Transform transform, int depth)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100, LayerMask.GetMask("Room")))
			{
				hitMarkers[depth].transform.SetPositionAndRotation(hit.point, transform.rotation);

				serTrackerManager.DrawLineRpc(
					depth,
					transform.position, hitMarkers[depth].transform.position
				);

				depth--;

				if (depth < 0 || !hit.transform.gameObject.CompareTag("InputPortal"))
				{
					hitMarkers[depth + 1].transform.forward = hit.normal;
					return;
				}

				Matrix4x4 teleportMatrix =
					 outputPortal.transform.localToWorldMatrix *
					 hit.transform.worldToLocalMatrix *
					hitMarkers[depth + 1].transform.localToWorldMatrix;

				hitMarkers[depth].transform.SetPositionAndRotation(
					teleportMatrix.GetColumn(3),
					teleportMatrix.rotation
				);

				RayCastAndTeleport(hitMarkers[depth].transform, depth - 1);

				hitMarkers[depth + 1].transform.forward = hit.normal;
			}
			else
			{
				serTrackerManager.DrawLineRpc(
					depth,
					transform.position, transform.position + transform.forward * 10
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