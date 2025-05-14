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
    private Transform grabbedObject;
    private readonly List<Transform> grabbingHands = new();
    private XRGrabInteractable grabInteractable;
    private XRHandSubsystem handSubsystem;
    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null && Camera.main != null)
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

        // if (!IsSufficientlyPinching(hand) || args.interactorObject.transform.gameObject.GetComponent<Socket>() != null)
        if (args.interactorObject.transform.gameObject.GetComponent<Socket>() != null)
        {
            return;
        }

        if (!grabbingHands.Contains(interactorTransform))
        {
            grabbingHands.Add(interactorTransform);
        }

        if (grabbedObject == null)
        {
            grabbedObject = transform;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (args.interactorObject.transform.gameObject.GetComponent<Socket>() != null)
        {
            return;
        }

        var interactorTransform = args.interactorObject.transform;
        grabbingHands.Remove(interactorTransform);

        if (grabbingHands.Count == 0)
        {
            grabbedObject = null;
        }
    }

    void Update()
    {
        if (IsOutOfView())
        {
            ForceReleaseGrabbedObject();
        }
    }

    private bool IsOutOfView()
    {
        if (grabbedObject == null)
        {
            return false;
        }

        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(grabbedObject.position);
        bool isOutOfView = viewportPoint.x < -marginX || viewportPoint.x > 1 + marginX ||
                       viewportPoint.y < -marginY || viewportPoint.y > 1 + marginY ||
                       viewportPoint.z < 0;
        return isOutOfView;
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

    bool IsSufficientlyPinching(XRHand hand)
    {
        XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
        XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);

        if (!thumbTip.TryGetPose(out var thumbPose) || !indexTip.TryGetPose(out var indexPose))
        {
            return true;
        }

        float distance = Vector3.Distance(thumbPose.position, indexPose.position);
        return distance < touchThreshold;
    }
}
