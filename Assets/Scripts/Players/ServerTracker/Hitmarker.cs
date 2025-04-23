using System.Collections.Generic;
using UnityEngine;

namespace Main
{
	class HitMarker : MonoBehaviour
	{
		const int MAX_CURVE_ITERATIONS = 40;
		const float ROTATE_SPEED = 30f;
		const float NOT_SHOWING_DEPTH = -10f;
		[SerializeField]
		GameObject child;
		[SerializeField]
		LineRenderer lineRenderer;
		[SerializeField]
		Material dashMaterial;
		[SerializeField]
		Material solidMaterial;
		float initialVelocity;
		int curveIterations = 0;
		float currentRotation = 0;

		void Start()
		{
			var meshColor = child.GetComponentInChildren<MeshRenderer>().material.color;
			lineRenderer.startColor = lineRenderer.endColor = meshColor;
		}

		void Update()
		{
			if (transform.position.y <= NOT_SHOWING_DEPTH)
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

		public bool IsShowing()
		{
			return transform.position.y > NOT_SHOWING_DEPTH / 2;
		}

		public void Hide()
		{
			transform.position = Vector3.up * NOT_SHOWING_DEPTH;
			lineRenderer.positionCount = 0;
		}

		public void Show(int bounceTimes, int numBounce, Vector3 start, Vector3 forward, Vector3 end)
		{
			curveIterations = bounceTimes > 0 ? 1 : MAX_CURVE_ITERATIONS;
			lineRenderer.positionCount = curveIterations + 1;

			var newMaterial = bounceTimes > 0 && numBounce > 0 ? dashMaterial : solidMaterial;
			if (lineRenderer.material != newMaterial)
			{
				lineRenderer.material = newMaterial;
			}

			var positions = new List<Vector3>();

			initialVelocity = Vector3.Distance(start, end) / 2;
			for (float ratio = 0; ratio <= 1; ratio += 1f / curveIterations)
			{
				positions.Add(Lerp(ratio, start, forward, end));
			}
			positions.Add(end);

			lineRenderer.SetPositions(positions.ToArray());
		}

		Vector3 Lerp(float ratio, Vector3 start, Vector3 forward, Vector3 end)
		{
			var tangent1 = Vector3.Lerp(
				start,
				start + forward * initialVelocity,
				ratio
			);
			var tangent2 = Vector3.Lerp(
				start + forward * initialVelocity,
				end,
				ratio
			);
			var curve = Vector3.Lerp(tangent1, tangent2, ratio);

			return curve;
		}
	}
}