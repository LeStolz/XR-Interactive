using UnityEngine;

namespace Multiplayer
{
	class Hitmarker : MonoBehaviour
	{
		[SerializeField]
		GameObject child;
		const float ROTATE_SPEED = 30f;
		float currentRotation = 0;

		void Start()
		{
			var lineRenderer = GetComponent<LineRenderer>();
			var meshColor = child.GetComponentInChildren<MeshRenderer>().material.color;
			lineRenderer.startColor = lineRenderer.endColor = meshColor;
		}

		void Update()
		{
			if (transform.position.y <= -5)
			{
				child.SetActive(false);
				return;
			}
			else
			{
				child.SetActive(true);
			}

			currentRotation = (currentRotation + ROTATE_SPEED * Time.deltaTime) % 360;
			child.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
		}
	}
}