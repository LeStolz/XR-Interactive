using UnityEngine;

namespace Multiplayer
{
	class HitMarkerVisuals : MonoBehaviour
	{
		[SerializeField]
		GameObject parent;
		const float ROTATE_SPEED = 20f;

		void Update()
		{
			transform.Rotate(transform.forward, ROTATE_SPEED * Time.deltaTime);
		}
	}
}