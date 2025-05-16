using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using Main;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Tile : NetworkBehaviour
{
    private readonly bool pinchingIsSufficientWhenPoseNotDetected = true;
    private readonly float marginX = 0.5f;
    private readonly float marginY = 0.6f;
    private readonly float pinchThreshold = 0.01f;
    private readonly float moveThreshold = 0.3f;

    private Vector3 prevPos;
    private Transform grabbedObject;
    private readonly List<Transform> grabbingHands = new();
    private XRGrabInteractable grabInteractable;
    private XRHandSubsystem handSubsystem;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null && Camera.main != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
        }

        prevPos = transform.position;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

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

    void LateUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        if ((prevPos - transform.position).magnitude > moveThreshold)
        {
            if (grabbedObject != null) ForceReleaseGrabbedObject();
            transform.position = prevPos;
        }

        if (grabbedObject != null && XRINetworkAvatar.IsOutOfView(grabbedObject, marginX, marginY))
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
            return pinchingIsSufficientWhenPoseNotDetected;
        }

        float distance = Vector3.Distance(thumbPose.position, indexPose.position);
        return distance < pinchThreshold;
    }

    [Rpc(SendTo.Everyone)]
    public void SetupRpc(Vector3 pos, Vector3 rot, string tileID, bool freeze)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        prevPos = pos;
        gameObject.GetComponent<Rigidbody>().constraints = freeze ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        gameObject.name = tileID;
    }

    public void ToggleVisibility(bool visible)
    {
        gameObject.GetComponentInChildren<MeshRenderer>().enabled = visible;
    }
}
