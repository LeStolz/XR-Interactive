using System.Collections.Generic;
using Main;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabDetector : MonoBehaviour
{
    [SerializeField] private readonly float moveThreshold = 0.2f;
    [SerializeField] private readonly float marginX = 0.5f;
    [SerializeField] private readonly float marginY = 0.6f;
    [SerializeField] private readonly float pinchThreshold = 0.005f;

    private Vector3 prevPos;

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

        prevPos = transform.position;
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

        if (!IsSufficientlyPinching(hand) || args.interactorObject.transform.gameObject.GetComponent<Socket>() != null)
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
        if (grabbedObject == null)
        {
            return;
        }

        if ((prevPos - transform.position).magnitude > moveThreshold)
        {
            ForceReleaseGrabbedObject();
            transform.position = prevPos;
        }
        else if (XRINetworkAvatar.IsOutOfView(grabbedObject, marginX, marginY))
        {
            ForceReleaseGrabbedObject();
        }

        prevPos = transform.position;
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
            return false;
        }

        float distance = Vector3.Distance(thumbPose.position, indexPose.position);
        return distance < pinchThreshold;
    }
}
