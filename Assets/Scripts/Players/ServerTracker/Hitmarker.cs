using System.Collections.Generic;
using UnityEngine;

namespace Main
{
	class HitMarker : MonoBehaviour
	{
		[SerializeField]
		GameObject child;
		[SerializeField]
		LineRenderer lineRenderer;
		[SerializeField]
		Material dashMaterial;
		[SerializeField]
		Material solidMaterial;
		float initialVelocity;
		const int MAX_CURVE_ITERATIONS = 40;
		int curveIterations = 0;
		const float ROTATE_SPEED = 30f;
		float currentRotation = 0;

		void Start()
		{
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

		public void DrawLine(int bounceTimes, int maxBounceTimes, Vector3 start, Vector3 end)
		{
			curveIterations = bounceTimes < maxBounceTimes ? 1 : MAX_CURVE_ITERATIONS;
			lineRenderer.positionCount = curveIterations + 1;

			var newMaterial = bounceTimes < maxBounceTimes ? dashMaterial : solidMaterial;
			if (lineRenderer.material != newMaterial)
			{
				lineRenderer.material = newMaterial;
			}

			var positions = new List<Vector3>();

			initialVelocity = Vector3.Distance(start, end) / 2;
			for (float ratio = 0; ratio <= 1; ratio += 1f / curveIterations)
			{
				positions.Add(Lerp(ratio, start, end));
			}
			positions.Add(end);

			lineRenderer.SetPositions(positions.ToArray());
		}

		Vector3 Lerp(float ratio, Vector3 start, Vector3 end)
		{
			var tangent1 = Vector3.Lerp(
				start,
				start + transform.forward * initialVelocity,
				ratio
			);
			var tangent2 = Vector3.Lerp(
				start + transform.forward * initialVelocity,
				end,
				ratio
			);
			var curve = Vector3.Lerp(tangent1, tangent2, ratio);

			return curve;
		}
	}
}