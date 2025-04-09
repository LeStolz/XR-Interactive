using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Socket : MonoBehaviour
{
    GameObject socket;

    void Awake()
    {
        transform.localScale = BoardGameManager.Instance.TilePrefabs[0].transform.localScale;
    }

    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactableObject == null)
        {
            return;
        }

        AttachObjectToSocket(args.interactableObject);

        socket = Instantiate(
            gameObject, transform.position + Vector3.up * transform.localScale.x, Quaternion.identity
        );
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactableObject == null)
        {
            return;
        }

        if (socket != null)
        {
            Destroy(socket);
            socket = null;
        }

        transform.localRotation = Quaternion.identity;
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
