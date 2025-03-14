using UnityEngine;

namespace Multiplayer
{
	class HitMarkerVisuals : MonoBehaviour
	{
		[SerializeField]
		GameObject parent;
		const float ROTATE_SPEED = 20f;
		float currentRotation = 0;

		void Update()
		{
			currentRotation += ROTATE_SPEED * Time.deltaTime;
			parent.transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
		}
	}
}