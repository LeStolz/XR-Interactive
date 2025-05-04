using System.Collections.Generic;
using Main;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabDetector : MonoBehaviour
{
	[SerializeField] private float marginX = 0.5f;
	[SerializeField] private float marginY = 0.6f;
	[SerializeField] private float touchThreshold = 0.005f;
	public Camera mainCamera;
	private Transform grabbedObject;
	private XRGrabInteractable grabInteractable;
	private XRHandSubsystem handSubsystem;
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

	void Start()
	{
		var subsystems = new List<XRHandSubsystem>();
		SubsystemManager.GetSubsystems(subsystems);
		if (subsystems.Count > 0)
		{
			handSubsystem = subsystems[0];
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
		var interactorTransform = args.interactorObject.transform;
		bool isLeftHand = interactorTransform.name.ToLower().Contains("left");

		XRHand hand = isLeftHand ? handSubsystem.leftHand : handSubsystem.rightHand;

		if (!IsTouching(hand))
		{
			return;
		}

		if (args.interactorObject.transform.gameObject.GetComponent<Socket>() != null)
		{
			return;
		}

		grabbedObject = transform;
	}

	void OnReleased(SelectExitEventArgs args)
	{
		if (args.interactorObject.transform.gameObject.GetComponent<Socket>() != null)
		{
			return;
		}

		grabbedObject = null;
	}

	void Update()
	{
		if (grabbedObject == null)
		{
			return;
		}
		Vector3 viewportPoint = mainCamera.WorldToViewportPoint(grabbedObject.position);

		bool isOutOfView = viewportPoint.x < -marginX || viewportPoint.x > 1 + marginX ||
					   viewportPoint.y < -marginY || viewportPoint.y > 1 + marginY ||
					   viewportPoint.z < 0;

		if (isOutOfView)
		{
			ForceReleaseGrabbedObject();
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

	public bool IsTouching(XRHand hand)
	{
		XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
		XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);

		if (thumbTip == null || indexTip == null)
		{
			return false;
		}

		Vector3 thumbPosition = thumbTip.TryGetPose(out var thumbPose) ? thumbPose.position : Vector3.zero;
		Vector3 indexPosition = indexTip.TryGetPose(out var indexPose) ? indexPose.position : Vector3.zero;

		float distance = Vector3.Distance(thumbPosition, indexPosition);

		return distance < touchThreshold;
	}
}