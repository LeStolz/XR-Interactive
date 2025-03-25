using UnityEngine;
using UnityEngine.UI;

namespace Multiplayer
{
	public class TrackerUI : MonoBehaviour
	{
		[SerializeField]
		Toggle[] trackerButtons;

		public void SetTrackers()
		{
			var serverTrackerManager = NetworkGameManager.Instance.FindPlayerByRole<ServerTrackerManager>(Role.ServerTracker);

			for (var i = 0; i < trackerButtons.Length; i++)
			{
				var j = i;

				trackerButtons[i].onValueChanged.AddListener((_) =>
				{
					serverTrackerManager.Calibrate(j);
				});
			}
		}
	}
}