using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SocketManager : MonoBehaviour
{
    public BoardManager boardManager;
    GameObject socket;

    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactableObject == null)
        {
            return;
        }

        AttachObjectToSocket(args.interactableObject);

        socket = boardManager.InstantiateSocket(
            transform.position + Vector3.up * (BoardManager.socketGap + socket.transform.localScale.x / 2f)
        );
    }

    public void OnSelectExited(SelectExitEventArgs args) {
        if (args.interactableObject == null)
        {
            return;
        }

        if (socket != null)
        {
            Destroy(socket);
            socket = null;
        }
    }

    private void AttachObjectToSocket(IXRSelectInteractable interactableObject)
    {
        Vector3 eulerAngles = interactableObject.transform.eulerAngles;

        eulerAngles.x = Mathf.Round(eulerAngles.x / 90f) * 90f;
        eulerAngles.y = Mathf.Round(eulerAngles.y / 90f) * 90f;
        eulerAngles.z = Mathf.Round(eulerAngles.z / 90f) * 90f;

        Quaternion normalizedRotation = Quaternion.Euler(eulerAngles);
        transform.localRotation = normalizedRotation;
    }
}
