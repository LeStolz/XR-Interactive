using UnityEngine;

namespace Multiplayer
{
	class TrackerManager : MonoBehaviour
	{
		[SerializeField]
		Camera outputPortal;
		[SerializeField]
		GameObject inputHitMarker;
		[SerializeField]
		GameObject inputHitMarkerThroughPortal;
		[SerializeField]
		GameObject outputHitMarker;

		void Update()
		{
			if (
				Physics.Raycast(
					transform.position, transform.forward,
					out RaycastHit hit, 100, LayerMask.GetMask("InputPortal")
			))
			{
				inputHitMarker.transform.SetPositionAndRotation(hit.point, transform.rotation);

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
				}

				inputHitMarker.transform.forward = hit.normal;
			}
		}
	}
}