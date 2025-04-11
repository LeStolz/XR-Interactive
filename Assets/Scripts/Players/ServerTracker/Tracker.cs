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

			RayCastAndTeleport(
				outputPortal,
				new Ray(
					arrow.transform.position,
					arrow.transform.forward
				),
				hitMarkers.Length - 1
			);
		}

		void Update()
		{
			if (!UpdateHitmarkers())
			{
				return;
			}

			int maxBounceTimes = hitMarkers.Count(hm => hm.transform.position.y >= -5) - 1;

			for (int i = 0; i < hitMarkers.Length; i++)
			{
				if (hitMarkers[i].transform.position.y < -5)
				{

					hitMarkers[i].DrawLine(
							i,
							maxBounceTimes,
							arrow.transform.position,
							arrow.transform.position + arrow.transform.forward * 10
						);
					continue;
				}

				hitMarkers[i].DrawLine(i, maxBounceTimes, arrow.transform.position, hitMarkers[i].transform.position);
			}
		}

		void RayCastAndTeleport(Camera outputPortal, Ray ray, int depth)
		{
			if (depth < 0)
			{
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Default")))
			{
				hitMarkers[depth].transform.position = hit.point;
				hitMarkers[depth].transform.forward = hit.normal;

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
				while (depth >= 0)
				{
					hitMarkers[depth].transform.position = new(0, -10, 0);
					depth--;
				}
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