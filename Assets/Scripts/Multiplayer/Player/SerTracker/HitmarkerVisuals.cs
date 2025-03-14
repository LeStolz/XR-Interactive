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
			currentRotation = (currentRotation + ROTATE_SPEED * Time.deltaTime) % 360;
			transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
		}
	}
}