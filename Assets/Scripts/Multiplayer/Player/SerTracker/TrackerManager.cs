using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
	class TrackerManager : MonoBehaviour
	{
		[SerializeField]
		GameObject outputPortal;
		[SerializeField]
		GameObject inputHitMarker;
		[SerializeField]
		GameObject inputHitMarkerThroughPortal;
		[SerializeField]
		GameObject outputHitMarker;
		[SerializeField]
		GameObject model;
		[SerializeField]
		SerTrackerManager serTrackerManager;

		public void SetOutputPortal(GameObject outputPortal)
		{
			this.outputPortal = outputPortal;
		}

		void Update()
		{
			model.transform.SetPositionAndRotation(transform.position, transform.rotation);

			if (outputPortal == null)
			{
				return;
			}

			if (
				Physics.Raycast(
					transform.position, transform.forward,
					out RaycastHit hit, 100, LayerMask.GetMask("InputPortal")
			))
			{
				inputHitMarker.transform.SetPositionAndRotation(hit.point, transform.rotation);
				serTrackerManager.DrawLineRpc(transform.position, inputHitMarker.transform.position, Color.red);

				Matrix4x4 teleportMatrix =
					 outputPortal.transform.localToWorldMatrix *
					 hit.transform.worldToLocalMatrix *
					inputHitMarker.transform.localToWorldMatrix;

				inputHitMarkerThroughPortal.transform.SetPositionAndRotation(
					teleportMatrix.GetColumn(3),
					teleportMatrix.rotation
				);

				if (
					Physics.Raycast(
						inputHitMarkerThroughPortal.transform.position,
						inputHitMarkerThroughPortal.transform.forward,
						out RaycastHit hitThroughPortal, 100
				))
				{
					outputHitMarker.transform.position = hitThroughPortal.point;
					outputHitMarker.transform.forward = hitThroughPortal.normal;
					serTrackerManager.DrawLineRpc(
						inputHitMarkerThroughPortal.transform.position,
						outputHitMarker.transform.position,
						Color.green
					);
				}
				else
				{
					outputHitMarker.transform.position = new(1000, 1000, 1000);
				}

				inputHitMarker.transform.forward = hit.normal;
			}
			else
			{
				inputHitMarker.transform.position = new(1000, 1000, 1000);
			}
		}
	}
}