using UnityEngine;

namespace Multiplayer
{
	class HitMarkerVisuals : MonoBehaviour
	{
		const float ROTATE_SPEED = 10f;

		void Update()
		{
			transform.Rotate(transform.forward, ROTATE_SPEED * Time.deltaTime);
		}
	}
}