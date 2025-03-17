using UnityEngine;

namespace Multiplayer
{
	class TrackerManager : MonoBehaviour
	{
		[field: SerializeField]
		public Hitmarker[] HitMarkers { get; private set; }
		[field: SerializeField]
		public GameObject Model { get; private set; }
		[field: SerializeField]
		public GameObject Arrow { get; private set; }
		[SerializeField]
		SerTrackerManager serTrackerManager;

		void Update()
		{
			if (!serTrackerManager.IsLocalOwner)
			{
				return;
			}

			Model.transform.SetPositionAndRotation(transform.position, transform.rotation);
		}

		public void DrawLine(int hitMarkerId, Vector3 start, Vector3 end)
		{
			var lineRenderer = HitMarkers[hitMarkerId].GetComponent<LineRenderer>();

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