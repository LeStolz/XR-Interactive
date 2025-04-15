using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabDetector : MonoBehaviour
{
	[SerializeField] private float marginX = 0.5f;
	[SerializeField] private float marginY = 0.6f;
	public Camera mainCamera;
	private Transform grabbedObject;
	private XRGrabInteractable grabInteractable;

	void Awake()
	{
		mainCamera = Camera.main;
		grabInteractable = GetComponent<XRGrabInteractable>();

		if (grabInteractable != null && mainCamera != null)
		{
			grabInteractable.selectEntered.AddListener(OnGrabbed);
			grabInteractable.selectExited.AddListener(OnReleased);
		}
	}

	void OnDestroy()
	{
		if (grabInteractable != null)
		{
			grabInteractable.selectEntered.RemoveListener(OnGrabbed);
			grabInteractable.selectExited.RemoveListener(OnReleased);
		}
	}

	void OnGrabbed(SelectEnterEventArgs args)
	{
		grabbedObject = transform;
	}

	void OnReleased(SelectExitEventArgs args)
	{
		grabbedObject = null;
	}

	void Update()
	{
		if (grabbedObject != null)
		{
			Vector3 viewportPoint = mainCamera.WorldToViewportPoint(grabbedObject.position);

			bool isOutOfView = viewportPoint.x < -marginX || viewportPoint.x > 1 + marginX ||
						   viewportPoint.y < -marginY || viewportPoint.y > 1 + marginY ||
						   viewportPoint.z < 0;

			if (isOutOfView)
			{
				ForceReleaseGrabbedObject();
			}
		}
	}

	void ForceReleaseGrabbedObject()
	{
		var interactable = grabbedObject.GetComponent<XRGrabInteractable>();
		if (interactable != null && interactable.isSelected)
		{
			interactable.interactionManager.CancelInteractableSelection(interactable as IXRSelectInteractable);
		}

		grabbedObject = null;
	}

}